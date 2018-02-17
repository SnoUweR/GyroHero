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

    public function __construct($settings)
    {


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

    /**
     * @param $sql mysqli
     * @param $hash string
     * @return bool
     */
    public static function check_hash($sql, $hash)
    {
        $stmt = $sql->prepare(
            "SELECT WorkerID FROM workers WHERE Hash = ?"
        );
        if ($stmt) {

            $stmt->bind_param('s', $hash);
            $stmt->execute();
            $selectCounts = $stmt->affected_rows;
            $stmt->close();

            return $selectCounts > 0;
        }
        return false;
    }

    /**
     * @param $sql mysqli
     * @param $hash string
     * @return bool
     */
    public static function get_workerid_by_hash($sql, $hash)
    {
        $stmt = $sql->prepare(
            "SELECT WorkerID FROM workers WHERE Hash = ?"
        );

        $workerId = -1;
        if ($stmt) {

            $stmt->bind_param('s', $hash);
            $stmt->execute();
            $stmt->bind_result($workerId);
            if (!$stmt->fetch()) {
                $workerId = -1;
            }
            $stmt->close();
        }
        return $workerId;
    }

    public function get_element($worker_id)
    {
        $data = [];
        $status = "200";
        $stmt = $this->_SQL->prepare(
            "SELECT Name, Location FROM workers WHERE WorkerID = ?"
        );
        if ($stmt) {

            $stmt->bind_param('i', $worker_id);
            $stmt->execute();
            $stmt->bind_result($name, $location);
            if ($stmt->fetch()) {
                $data = [
                    "Name" => $name,
                    "Location" => $location,
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
            "SELECT WorkerID, Name, Location FROM workers", MYSQLI_USE_RESULT
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

    private function get_hash_by_id($worker_id)
    {
        $stmt = $this->_SQL->prepare(
            "SELECT Hash FROM workers WHERE WorkerID = ?"
        );
        if ($stmt) {

            $stmt->bind_param('i', $worker_id);
            $stmt->execute();
            $stmt->bind_result($hash);
            if (!$stmt->fetch()) {
                $hash = null;
            }
            $stmt->close();

            return $hash;
        }
        return null;
    }

    public function insert($name, $location)
    {
        $status = "200";
        $stmt = $this->_SQL->prepare(
            "INSERT INTO workers (Name, Location) VALUES (?, ?)"
        );

        if ($stmt)
        {
            $stmt->bind_param('ss', $name, $location);
            $stmt->execute();

            if ($stmt->affected_rows < 1)
            {
                $status = "406";
            }

            $worker_id = $stmt->insert_id;
            $stmt->close();

            $hash = $this->get_hash_by_id($worker_id);

            return $this->return_json_result(array(
                "WorkerID" => $worker_id,
                "Hash" => $hash,
            ), $status);
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