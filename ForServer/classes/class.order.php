<?php
/**
 * Created by PhpStorm.
 * User: SnoUweR
 * Date: 12.02.2018
 * Time: 10:29
 */

class Order
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
        //$this->_SQL = new simpleMysqli($settings);
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

    public function get_order($order_id)
    {
        $data = [];
        $status = "200";
        $stmt = $this->_SQL->prepare(
            "SELECT Total, BeginTime, ClientName, WorkerID FROM orders WHERE OrderID = ?"
        );
        if ($stmt) {

            $stmt->bind_param('i', $order_id);
            $stmt->execute();
            $stmt->bind_result($total, $begin_time, $client_name, $worker_id);
            if ($stmt->fetch()) {
                $data = [
                    "Total" => $total,
                    "BeginTime" => $begin_time,
                    "ClientName" => $client_name,
                    "WorkerID" => $worker_id,
                ];
            } else {
                $data = "No order with passed OrderID";
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

    public function get_orders()
    {
        $result = $this->_SQL->query(
            "SELECT * FROM orders", MYSQLI_USE_RESULT
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

    public function insert_order($worker_id, $total, $client_name)
    {
        $status = "200";

        $stmt = $this->_SQL->prepare(
            "INSERT INTO orders (WorkerID, Total, ClientName) VALUES (?, ?, ?)"
        );

        if ($stmt)
        {
            $stmt->bind_param('ids', $worker_id, $total, $client_name);
            $stmt->execute();

            if ($stmt->affected_rows < 1)
            {
                $status = "406";
            }

            $stmt->close();
        }
        else
        {
            return $this->return_json_result([], "500");
        }
        return $this->return_json_result([], $status);
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