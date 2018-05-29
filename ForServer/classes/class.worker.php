<?php
/**
 * Created by PhpStorm.
 * User: SnoUweR
 * Date: 17.02.2018
 * Time: 10:51
 */
include_once "class.error_codes.php";

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
        $status = HtmlErrorCode::OK;
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
                $status = HtmlErrorCode::NOT_FOUND;
            }
            $stmt->close();
        }
        else
        {
            return $this->return_json_result("Internal Error", HtmlErrorCode::INTERNAL_SERVER_ERROR);
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
            return $this->return_json_result([], HtmlErrorCode::INTERNAL_SERVER_ERROR);
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

    public function insert($name, $location, $device_token)
    {
        $status = HtmlErrorCode::OK;
        $stmt = $this->_SQL->prepare(
            "INSERT INTO workers (Name, Location, DeviceToken) VALUES (?, ?, ?)"
        );

        if ($stmt)
        {
            $stmt->bind_param('sss', $name, $location, $device_token);
            $stmt->execute();

            if ($stmt->affected_rows < 1)
            {
                $status = HtmlErrorCode::NOT_ACCEPTABLE;
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
            return $this->return_json_result([], HtmlErrorCode::INTERNAL_SERVER_ERROR);
        }

    }

    public function get_all_device_tokens()
    {
        $result = $this->_SQL->query(
            "SELECT DeviceToken FROM workers WHERE DeviceToken IS NOT NULL", MYSQLI_USE_RESULT
        );
        if ($result) {
            $res_array = $result->fetch_all(MYSQLI_ASSOC);
            $result->free();
            return $res_array;
        }
        else
        {
            return $this->return_json_result([], HtmlErrorCode::INTERNAL_SERVER_ERROR);
        }
    }

    public function get_device_token($worker_id)
    {
        if ($worker_id < 0) {
            throw new InvalidArgumentException();
        }

        $stmt = $this->_SQL->prepare(
            "SELECT DeviceToken FROM workers WHERE WorkerID = ?"
        );

        $device_token = null;
        if ($stmt) {

            $stmt->bind_param('i', $worker_id);
            $stmt->execute();
            $stmt->bind_result($device_token);
            if (!$stmt->fetch()) {

            }
            $stmt->close();
        }
        return $device_token;
    }

    public function update_device_token($hash, $device_token)
    {
        $status = HtmlErrorCode::OK;

        $worker_id = GyroWorker::get_workerid_by_hash($this->_SQL, $hash);
        if ($worker_id < 0) {
            return $this->return_json_result([], HtmlErrorCode::UNAUTHORIZED);
        }

        $stmt = $this->_SQL->prepare(
            "UPDATE workers SET DeviceToken = ? WHERE WorkerID = ?"
        );

        if ($stmt)
        {
            $stmt->bind_param('si', $device_token, $worker_id);
            $stmt->execute();

            if ($stmt->affected_rows < 1)
            {
                $status = HtmlErrorCode::NOT_ACCEPTABLE;
            }
            $stmt->close();
            return $this->return_json_result([], $status);
        }
        else
        {
            return $this->return_json_result([], HtmlErrorCode::INTERNAL_SERVER_ERROR);
        }
    }

    private function return_json_result($data = [], $status = HtmlErrorCode::OK)
    {
        $res_array = [
            "status" => $status,
            "data" => $data,
        ];
        return json_encode($res_array);
    }
}