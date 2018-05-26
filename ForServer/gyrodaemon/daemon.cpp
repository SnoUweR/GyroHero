#if !defined(_GNU_SOURCE)
#define _GNU_SOURCE
#endif

/*
 * Код данного демона в большей степени основан на коде с Хабра:
 * https://habrahabr.ru/post/129207/
 * Данный демон выполняет простую функцию: 
 * 1) Читает конфигурационный файл формата:
 *    /путь/до/папки/обработки/
 *    периодичность_проверки_папки_в_секундах
 * 2) Согласно периодичности проверяет в папке новые файлы.
 * 3) Если есть файлы в папке, то через iconv конвертирует их из кодировки
 * KOI8-R в UTF8.
 * 4) Помещает сконвертированные файлы в папку out/ внутри папки обработки.
 * 5) Удаляет исходный файл.
 * 6) "Засыпает" на то время, пока не придет следующую период проверки.
 * 
 * Для работы демона необходима установленная утилита iconv.
 * Для запуска демона нужно просто запустить скомпилированную версию этого файла
 * с одним аргументом - путем к конфигурационному файлу.
 * Например, ./my_daemon ~/config.cfg
 * Для остановки демона нужно запустить скомпилированную версию этого файла
 * с аргументом -stop
 * Для обновления конфигурационного файла нужно зпустить программу с аргументом
 * -reload, либо самому отправить запущенному демону код SIGHUP.
 * Компилировать с параметром -lpthread. Например:
 * g++ -lpthread -o simpledaemon simple_daemon.cpp
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/file.h>
#include <execinfo.h>
#include <unistd.h>
#include <errno.h>
#include <wait.h>
#include <signal.h>
#include <sys/time.h>
#include <sys/resource.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <pthread.h>
#include <dirent.h>
#include <iconv.h>
#include <math.h>
#include <cctype>
#include <sstream>
#include <curlpp/cURLpp.hpp>
#include <curlpp/Easy.hpp>
#include <curlpp/Options.hpp>
#include <chrono>
#include <ctime>

// лимит для установки максимально кол-во открытых дескрипторов
#define FD_LIMIT   1024*10

// константы для кодов завершения процесса
#define CHILD_NEED_WORK   1
#define CHILD_NEED_TERMINATE 2

// для того, чтоб можно было корректно остановить демон после запуска,
// он должен иметь свой pid файл. Тут мы его указываем (создастся сам)
/*
 * Важно отметить, что в среде linux привычно, что демоны записывают
 * свой pid файл в директорию /var/run, которая, к тому же, после перезапуска
 * системы очищается. Но запись в /var/run доступна только руту.
 * Запускать данный демон из под рута - странно, потому что он делает
 * очень минимальный набор действий. Поэтому есть несколько вариантов:
 * 1) Каждый раз запускаться из под рута, создавать папку в /var/run, ствить на неё
 * права доступа, доступные обычному пользовтелю, а затем переключаться через su
 * на обычного пользователя и работать с ней полноценно.
 * 2) Записывать в свою локальную директорию.
 * Так как демон сделан лишь для примера, то было решено использовать второй способ.
 */
//#define PID_FILE "/var/run/my_daemon.pid"
#define PID_FILE "/opt/simple_daemon.pid"
// константы для хранения лог файлов мастер-процесса и демона
#define LOG_FILE "/opt/simple_daemon.log"
#define LOG_CHILD_FILE "/opt/child_daemon.log"

// глобальные переменные для временного хранения путей
// результирующей папки (куда будут перемещаться преобразованные файлы)
// и конфигурационного файла (оттуда будет перечитываться конфиг при соотв. команде)

// глобальная переменная для потока, в котором будет происходить вся "полезная
// работа" демона
pthread_t thread1;

// Время начала рабочего дня
tm start_time_struct;
// время конца рабочего дня
tm end_time_struct;

// функция записи в мастер-лог

void WriteLog(char* Msg) {
    FILE * fp;
    fp = fopen(LOG_FILE, "a");
    fprintf(fp, Msg);
    fclose(fp);
}

// функция записи в лог демона

void WriteChildLog(char* Msg) {
    FILE * fp;
    fp = fopen(LOG_CHILD_FILE, "a");
    fprintf(fp, Msg);
    fclose(fp);
}

/* Данная функция убирает пробелы из конца и начала строки.
 * Обрезання строка хранится в буфере out, который указывается в аргументах.
 * Поэтому данный буфер должен быть достаточного размера для хранения строки.
 * Если обрезанная строка будет больше буфера, то в него запишется лишь та часть
 * строки, которая туда помещается.
 * Возвращает функция новый размер строки str после её обрезки
 * Код взят отсюда:
 * https://stackoverflow.com/questions/122616/
 */
size_t trimwhitespace(char *out, size_t len, const char *str) {
    if (len == 0) {
        return 0;
    }

    const char *end;
    size_t out_size;

    /* Проходим по каждому символу строки, пока не найдем
     * символ, НЕ РАВНЫЙ пробелу. Когда мы нашли такой символ,
     * то указатель str будет указывать как раз него, что позволит
     * "пропустить" все пробелы, которые были в начале строки
     */
    while (isspace((unsigned char) *str)) {
        str++;
    }

    /* Если указатель str после предыдущего цикла указывает на 
     * нуль-символ, то значит мы дошли до конца строки, а значит строка 
     * изначально содержала в себе только пробелы и можно дальше не обрабатывать её
     */
    if (*str == 0) {
        *out = 0;
        return 1;
    }

    /* Убираем пробелы с конца.
     * Цикл работает по тому же принципу, что и предыдущий, но 
     * идет от конца строки 
     */
    end = str + strlen(str) - 1;
    while (end > str && isspace((unsigned char) *end)) {
        end--;
    }
    end++;

    // Размер результирующей строки будет минимальным размером от обрезанной строки 
    // и размером буфера - 1.
    out_size = (end - str) < len - 1 ? (end - str) : len - 1;

    // Копируем получшуюся строку в выходной буфер, а в конец добавляем нуль-символ
    memcpy(out, str, out_size);
    out[out_size] = 0;

    return out_size;
}


// функция для остановки потоков и освобождения ресурсов

void DestroyWorkThread() {
    pthread_cancel(thread1);
}

/*
 * Данная функция выполняется потоком thread1, 
 * который запускается демоном. Она содержит в себе основной алгоритм действий
 * демона - чтение папки, конвертация файлов в ней, перемещение файла в выходную
 * папку, удаление файла из исходной папки.
 */
void * child_thread_func(void *arg) {
    while (true) {
        WriteChildLog("Thread iterate\n");
        
        check_for_start_or_end();

        // пусть поток "спит" пока не придет время до новой проверки
        sleep(60);
    }
}

// функция которая инициализирует рабочие потоки

int InitWorkThread() {
    int id1 = 1;
    int result = pthread_create(&thread1, NULL, child_thread_func, &id1);
    if (result != 0) {
        WriteChildLog("Creating the first thread\n");
        return EXIT_FAILURE;
    }
    return result;
}


// функция обработки сигналов

static void signal_error(int sig, siginfo_t *si, void *ptr) {
    void* ErrorAddr;
    void* Trace[16];
    int x;
    int TraceSize;
    char** Messages;

    // запишем в лог что за сигнал пришел
    WriteLog("[DAEMON] Signal: %s");
    WriteLog(strsignal(sig));


#if __WORDSIZE == 64 // если дело имеем с 64 битной ОС
    // получим адрес инструкции которая вызвала ошибку
    ErrorAddr = (void*) ((ucontext_t*) ptr)->uc_mcontext.gregs[REG_RIP];
#else
    // получим адрес инструкции которая вызвала ошибку
    ErrorAddr = (void*) ((ucontext_t*) ptr)->uc_mcontext.gregs[REG_EIP];
#endif

    // произведем backtrace чтобы получить весь стек вызовов
    TraceSize = backtrace(Trace, 16);
    Trace[1] = ErrorAddr;

    // получим расшифровку трасировки
    Messages = backtrace_symbols(Trace, TraceSize);
    if (Messages) {
        WriteLog("== Backtrace ==\n");

        // запишем в лог
        for (x = 1; x < TraceSize; x++) {
            WriteLog(Messages[x]);
            WriteLog("\n");
        }

        WriteLog("== End Backtrace ==\n");
        free(Messages);
    }

    WriteLog("[DAEMON] Stopped\n");

    // остановим все рабочие потоки и корректно закроем всё что надо
    DestroyWorkThread();

    // завершим процесс с кодом требующим перезапуска
    exit(CHILD_NEED_WORK);
}


// функция установки максимального кол-во дескрипторов которое может быть открыто 

int SetFdLimit(int MaxFd) {
    struct rlimit lim;
    int status;

    // зададим текущий лимит на кол-во открытых дискриптеров
    lim.rlim_cur = MaxFd;
    // зададим максимальный лимит на кол-во открытых дискриптеров
    lim.rlim_max = MaxFd;

    // установим указанное кол-во
    status = setrlimit(RLIMIT_NOFILE, &lim);

    return status;
}

int WorkProc() {
    struct sigaction sigact;
    sigset_t sigset;
    int signo;
    int status;

    // сигналы об ошибках в программе будут обрататывать более тщательно
    // указываем что хотим получать расширенную информацию об ошибках
    sigact.sa_flags = SA_SIGINFO;
    // задаем функцию обработчик сигналов
    sigact.sa_sigaction = signal_error;

    sigemptyset(&sigact.sa_mask);

    // установим наш обработчик на сигналы

    sigaction(SIGFPE, &sigact, 0); // ошибка FPU
    sigaction(SIGILL, &sigact, 0); // ошибочная инструкция
    sigaction(SIGSEGV, &sigact, 0); // ошибка доступа к памяти
    sigaction(SIGBUS, &sigact, 0); // ошибка шины, при обращении к физической памяти

    sigemptyset(&sigset);

    // блокируем сигналы которые будем ожидать
    // сигнал остановки процесса пользователем
    sigaddset(&sigset, SIGQUIT);

    // сигнал для остановки процесса пользователем с терминала
    sigaddset(&sigset, SIGINT);

    // сигнал запроса завершения процесса
    sigaddset(&sigset, SIGTERM);

    get_constants();


    sigprocmask(SIG_BLOCK, &sigset, NULL);

    // Установим максимальное кол-во дискрипторов которое можно открыть
    SetFdLimit(FD_LIMIT);

    // запишем в лог, что наш демон стартовал
    WriteChildLog("[DAEMON] Started\n");

    // запускаем все рабочие потоки
    status = InitWorkThread();
    if (!status) {
        // цикл ожижания сообщений
        for (;;) {
            // ждем указанных сообщений
            sigwait(&sigset, &signo);

            break;
        }

        // остановим все рабочеи потоки и корректно закроем всё что надо
        DestroyWorkThread();
    } else {
        WriteChildLog("[DAEMON] Create work thread failed\n");
    }

    WriteChildLog("[DAEMON] Stopped\n");

    // вернем код не требующим перезапуска
    return CHILD_NEED_TERMINATE;
}

// функция создания pid-файла, который содержит в себе
// pid - идентификатор процесса с демоном

void SetPidFile(char* Filename) {
    FILE* f;

    f = fopen(Filename, "w+");
    if (f) {
        fprintf(f, "%u", getpid());
        fclose(f);
    }
}

// функция, которая осуществляет слежение за процессом-демоном

int MonitorProc() {
    WriteChildLog("MONITOR PROC\n");
    int pid;
    int status;
    int need_start = 1;
    sigset_t sigset;
    siginfo_t siginfo;

    // настраиваем сигналы которые будем обрабатывать
    sigemptyset(&sigset);

    // сигнал остановки процесса пользователем
    sigaddset(&sigset, SIGQUIT);

    // сигнал для остановки процесса пользователем с терминала
    sigaddset(&sigset, SIGINT);

    // сигнал запроса завершения процесса
    sigaddset(&sigset, SIGTERM);

    // сигнал посылаемый при изменении статуса дочернего процесс
    sigaddset(&sigset, SIGCHLD);

    sigprocmask(SIG_BLOCK, &sigset, NULL);

    // данная функция создат файл с нашим PID'ом
    SetPidFile(PID_FILE);


    // бесконечный цикл работы
    for (;;) {
        // если необходимо создать потомка
        if (need_start) {
            // создаём потомка
            pid = fork();
        }

        need_start = 1;

        if (pid == -1) // если произошла ошибка
        {
            // запишем в лог сообщение об этом
            WriteChildLog("[MONITOR] Fork failed (%s)\n");
            WriteChildLog(strerror(errno));
            WriteChildLog("\n");
        }
        else if (!pid) // если мы потомок
        {
            // данный код выполняется в потомке

            // запустим функцию отвечающую за работу демона
            status = WorkProc();

            // завершим процесс
            exit(status);
        }
        else // если мы родитель
        {
            // данный код выполняется в родителе

            // ожидаем поступление сигнала
            sigwaitinfo(&sigset, &siginfo);

            // если пришел сигнал от потомка
            if (siginfo.si_signo == SIGCHLD) {
                // получаем статус завершение
                wait(&status);

                // преобразуем статус в нормальный вид
                status = WEXITSTATUS(status);

                // если потомок завершил работу с кодом говорящем о том, что не нужно дальше работать
                if (status == CHILD_NEED_TERMINATE) {
                    // запишем в лог сообщени об этом
                    WriteChildLog("[MONITOR] Childer stopped\n");

                    // прервем цикл
                    break;
                } else if (status == CHILD_NEED_WORK) // если требуется перезапустить потомка
                {
                    // запишем в лог данное событие
                    WriteChildLog("[MONITOR] Childer restart\n");
                }
            } else if (siginfo.si_signo == SIGHUP) // если пришел сигнал что необходимо перезагрузить конфиг
            {
                kill(pid, SIGHUP); // перешлем его потомку
                need_start = 0; // установим флаг что нам не надо запускать потомка заново
            } else // если пришел какой-либо другой ожидаемый сигнал
            {
                // запишем в лог информацию о пришедшем сигнале
                WriteChildLog("[MONITOR] Signal ");
                WriteChildLog(strsignal(siginfo.si_signo));
                WriteChildLog("\n");

                // убьем потомка
                kill(pid, SIGTERM);
                status = 0;
                break;
            }
        }
    }

    // запишем в лог, что мы остановились
    WriteChildLog("[MONITOR] Stopped\n");

    // удалим файл с PID'ом
    unlink(PID_FILE);

    return status;
}

void send_push(char title[512], char body[512]) {
    std::stringstream response;
    curlpp::Easy foo;
    foo.setOpt( new curlpp::options::Url( "http://gyro.snouwer.ru/push/8" ) );

    std::list<std::string> header; 
    header.push_back("Content-Type: application/x-www-form-urlencoded"); 

    foo.setOpt(new curlpp::options::HttpHeader(header)); 
    char postVars[1024];
     sprintf(postVars, "title=%s&body=%s",
            title,body);

    std::cout << postVars << std::endl;

    foo.setOpt(new curlpp::options::PostFields(postVars));
    foo.setOpt(new curlpp::options::PostFieldSize(strlen(postVars)));

    foo.setOpt( new curlpp::options::WriteStream( &response ) );
    foo.perform();
    std::cout << response.str() << std::endl;
}

void get_constants()
{
    std::stringstream response;
    curlpp::Cleanup cleaner;

    curlpp::Easy startRequest;
    startRequest.setOpt( new curlpp::options::Url( "http://gyro.snouwer.ru/config/start/" ) );
    startRequest.setOpt( new curlpp::options::WriteStream( &response ) );
    startRequest.perform();
    strptime(response.str().c_str(), "%H:%M:%S", &start_time_struct);

    response.str("");
    response.clear();
    curlpp::Easy endRequest;
    endRequest.setOpt( new curlpp::options::Url( "http://gyro.snouwer.ru/config/end/" ) );
    endRequest.setOpt( new curlpp::options::WriteStream( &response ) );
    endRequest.perform();
    strptime(response.str().c_str(), "%H:%M:%S", &end_time_struct);
}

void check_for_start_or_end()
{
    auto cur_time = std::chrono::system_clock::now();
    std::time_t time_t_curtime = std::chrono::system_clock::to_time_t(cur_time);
    auto tm_struct = std::localtime(&time_t_curtime);
    std::cout << "meow";
    if ((start_time_struct.tm_hour == tm_struct->tm_hour &&
        start_time_struct.tm_min == tm_struct->tm_min) ||
        true)
        {
            send_push("Новый рабочий день", "Начался новый рабочий день. Удачи!");
        }

    else if ((end_time_struct.tm_hour == tm_struct->tm_hour &&
        end_time_struct.tm_min == tm_struct->tm_min) ||
        true)
        {
            send_push("Конец рабочего дня", "Рабочий день закончился. Пересчитайте деньги.");
        }
}

// основная функция, которая выполняется при запуске программы

int main(int argc, char** argv) {
    
    int status;
    int pid;

    // если параметров командной строки меньше двух, то покажем как использовать демона
    if (strcmp("-stop", argv[1]) == 0)
    {
        printf("Stopping daemon...\n");
        char commandForKill[256];
        strcat(commandForKill, "pkill -15 -F "); // отправляем сигнл 15 - sigterm
        strcat(commandForKill, PID_FILE);
        system(commandForKill);
        return 0;
    }

    // создаем потомка
    pid = fork();

    if (pid == -1) // если не удалось запустить потомка
    {
        // выведем на экран ошибку и её описание
        printf("Start Daemon Error: %s\n", strerror(errno));

        return -1;
    } else if (!pid) // если это потомок
    {
        WriteChildLog("CHILD HI\n");
        // данный код уже выполняется в процессе потомка
        // разрешаем выставлять все биты прав на создаваемые файлы,
        // иначе у нас могут быть проблемы с правами доступа
        umask(0);

        // создаём новый сеанс, чтобы не зависеть от родителя
        setsid();

        // переходим в корень диска, если мы этого не сделаем, то могут быть проблемы.
        // к примеру с размантированием дисков
        chdir("/");

        // закрываем дискрипторы ввода/вывода/ошибок, так как нам они больше не понадобятся
        close(STDIN_FILENO);
        close(STDOUT_FILENO);
        close(STDERR_FILENO);

        // Данная функция будет осуществлять слежение за процессом
        status = MonitorProc();

        return status;
    } else // если это родитель
    {
        /* sleep тут добавлен потому что иногда потомок не успевал полноценно
         * "форкнуться" и выполнить setsid() - отвязку от сеанса и других процессов, 
         * а родитель уже завершался. В таком случае всё работало некорректно */
         
        sleep(3);
        // завершим процес, т.к. основную свою задачу (запуск демона) мы выполнили
        return 0;
    }
}



