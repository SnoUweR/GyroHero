<?php
/**
 * Created by PhpStorm.
 * User: SnoUweR
 * Date: 17.02.2018
 * Time: 10:51
 */

class GyroWorker
{
    private $_SQL;

    public function __construct()
    {

        $settings = array(
            'server' => 'localhost',
            'username' => 'gyrohero',
            'password' => 'some_password',
            'db' => 'gyro_hero',
            'port' => 3306,
            'charset' => 'utf8',
        );

        $this->_SQL =
            mysqli_connect($settings['server'], $settings['username'], $settings['password'], $settings['db']);

        $this->_SQL->set_charset($settings['charset']);

        if (!$this->_SQL) {
            printf("Не удалось подключиться: %s\n", mysqli_connect_error());
            exit();
        }
    }

    function __destruct()
    {
        if ($this->_SQL) {
            $this->_SQL->close();
        }
    }

    public function get_element($worker_id)
    {
        $data = [];
        $status = "200";
        $stmt = $this->_SQL->prepare(
            "SELECT FirstName, SecondName, MiddleName FROM workers WHERE WorkerID = ?"
        );
        if ($stmt) {

            $stmt->bind_param('i', $worker_id);
            $stmt->execute();
            $stmt->bind_result($firstName, $secondName, $middleName);
            if ($stmt->fetch()) {
                $data = [
                    "FirstName" => $firstName,
                    "SecondName" => $secondName,
                    "MiddleName" => $middleName,
                ];
            } else {
                $data = "No worker with passed WorkerID";
                $status = "404";
            }
            $stmt->close();
        }
        else
        {
            return $this->return_json_result("Internal Error", "500");
        }
        return $this->return_json_result($data, $status);
    }

    public function get_all()
    {
        $result = $this->_SQL->query(
            "SELECT * FROM workers", MYSQLI_USE_RESULT
        );
        if ($result) {
            $res_array = $result->fetch_all(MYSQLI_ASSOC);
            $result->free();
            return $this->return_json_result($res_array);
        }
        else
        {
            return $this->return_json_result([], "500");
        }


    }

    public function insert($first_name, $second_name, $middle_name)
    {
        $status = "200";
        $stmt = $this->_SQL->prepare(
            "INSERT INTO workers (FirstName, SecondName, MiddleName) VALUES (?, ?, ?)"
        );

        if ($stmt)
        {
            $stmt->bind_param('sss', $first_name, $second_name, $middle_name);
            $stmt->execute();

            if ($stmt->affected_rows < 1)
            {
                $status = "406";
            }

            $worker_id = $stmt->insert_id;
            $stmt->close();
            return $this->return_json_result(array("WorkerID" => $worker_id), $status);
        }
        else
        {
            return $this->return_json_result([], "500");
        }

    }


    private function return_json_result($data = [], $status = "200")
    {
        $res_array = [
            "status" => $status,
            "data" => $data,
        ];
        return json_encode($res_array);
    }
}