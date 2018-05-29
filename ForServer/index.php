<?php

require __DIR__ . '/vendor/autoload.php';
include_once "classes/class.push_notifies.php";
include_once "classes/class.order.php";
include_once "classes/class.worker.php";
$method = $_SERVER['REQUEST_METHOD'];
$paths = explode("/", $_GET['_url']);
//для удаления пустого элемента в начале массива после Explode
array_shift($paths);
$resource = array_shift($paths);

$service_settings = array(
    'api_key' => 'Odka21349lsldaDe',
);

$settings = array(
    'server' => '193.124.181.25',
    'username' => 'gyrohero',
    'password' => 'n16vLGG7dG4JK75hENIF',
    'db' => 'gyro_hero',
    'port' => 3306,
    'charset' => 'utf8',
);

if ($resource == 'orders') {
    $name = array_shift($paths);

    if (empty($name)) {
        handle_orders_base($method);
    } else {
        handle_orders_name($method, $name);
    }
}
else if ($resource == 'workers') {
    $name = array_shift($paths);

    if (empty($name)) {
        handle_workers_base($method);
    } else {
        handle_workers_name($method, $name);
    }
}
else if ($resource == 'config') {
    $name = array_shift($paths);

    if (empty($name)) {

    } else {
        if ($name == 'start') {
            echo '09:00:00';
        }
        else if ($name == 'end') {
            echo '18:00:00';
        }
    }
}
else if ($resource == 'push') {
    $worker_id = array_shift($paths);

    if (empty($worker_id)) {
        handle_push_base($method);
    } else {
        handle_push_workerid($method, $worker_id);
    }
}
else  {
// We only handle resources under 'orders'
    header('HTTP/1.1 404 Not Found');
}

// Push
function handle_push_base($method)
{
    global $service_settings;
    global $settings;

    switch($method) {
        case 'POST':

            if (!isset($_POST['api_key']))
            {
                header('HTTP/1.1 406 Not acceptable');
                header('Api Key should set');
                break;
            }

            $api_key = $_POST["api_key"];
            if ($api_key !== $service_settings['api_key'])
            {
                header('HTTP/1.1 401 Unauthorized');
                header('Your API Key is incorrect');
                return;
            }

            if (!isset($_POST['title']))
            {
                header('HTTP/1.1 406 Not acceptable');
                header('Title should set');
                break;
            }
            $title = $_POST['title'];

            if (!isset($_POST['body']))
            {
                header('HTTP/1.1 406 Not acceptable');
                header('Body should set');
                break;
            }
            $body = $_POST['body'];

            $api = new GyroWorker($settings);
            $all_tokens = $api->get_all_device_tokens();
            foreach ($all_tokens as $token)
            {
                var_dump($token);
                $push = new PushNotify();
                $push->SendMessage($token["DeviceToken"], $title, $body);

            }

            header('HTTP/1.1 200 OK');
            break;

        default:
            header('HTTP/1.1 405 Method Not Allowed');
            header('Allow: POST');
            break;
    }
}

function handle_push_workerid($method, $worker_id)
{
    global $service_settings;
    global $settings;
    $api = new GyroWorker($settings);
    $push = new PushNotify();

    switch($method) {
        case 'POST':
            var_dump($_POST);
            $device_token = $api->get_device_token($worker_id);
            if ($device_token == null)
            {
                header('HTTP/1.1 406 Not acceptable');
                header('Worker doesnt have device token');
                break;
            }

            if (!isset($_POST['title']))
            {
                header('HTTP/1.1 406 Not acceptable');
                header('Title should set');
                break;
            }
            $title = $_POST['title'];

            if (!isset($_POST['body']))
            {
                header('HTTP/1.1 406 Not acceptable');
                header('Body should set');
                break;
            }
            $body = $_POST['body'];

            $result = $push->SendMessage(
                $device_token, $title, $body);

            echo $result;
            break;

        default:
            header('HTTP/1.1 405 Method Not Allowed');
            header('Allow: POST');
            break;
    }
}

// Workers
function handle_workers_base($method)
{
    global $settings;
    $api = new GyroWorker($settings);

    switch ($method) {
        case 'POST':
            //TODO: в дальнейшем тут нужны проверки на аргументы
            $name = $_POST["name"];
            $location = $_POST["location"];
            $device_token = $_POST["device_token"];
            return_json($api->insert($name, $location, $device_token));
            break;
        case 'GET':
            return_json($api->get_all());
            break;
        default:
            header('HTTP/1.1 405 Method Not Allowed');
            header('Allow: GET, POST');
            break;
    }
}

function handle_workers_name($method, $arg)
{
    global $settings;
    $api = new GyroWorker($settings);

    switch($method) {
        case 'POST':
            // Когда-нибудь тут будет инсерт нового
            break;

        case 'DELETE':
            // Когда-нибудь тут будет удаление
            break;

        case 'GET':
            return_json($api->get_element($arg));
            break;

        default:
            header('HTTP/1.1 405 Method Not Allowed');
            header('Allow: GET, POST, DELETE');
            break;
    }
}

// Orders
function handle_orders_base($method)
{
    global $settings;
    $api = new Order($settings);
    switch ($method) {
        case 'POST':
            //TODO: в дальнейшем тут нужны проверки на аргументы
            $hash = $_POST["hash"];
            $total = $_POST["total"];
            $client_name = $_POST["client_name"];
            return_json($api->insert_order($hash, $total, $client_name));
            break;
        case 'GET':
            return_json($api->get_orders());
            break;
        default:
            header('HTTP/1.1 405 Method Not Allowed');
            header('Allow: GET, POST');
            break;
    }
}

function handle_orders_name($method, $arg)
{
    global $settings;
    $api = new Order($settings);

    switch($method) {
        case 'POST':
            // Когда-нибудь тут будет инсерт нового
            break;

        case 'DELETE':
            // Когда-нибудь тут будет удаление
            break;

        case 'GET':
            return_json($api->get_order($arg));
            break;

        default:
            header('HTTP/1.1 405 Method Not Allowed');
            header('Allow: GET, POST, DELETE');
            break;
    }
}

function return_json($json)
{
    header('Content-Type: application/json');
    echo $json;
}
