<?php

include_once "classes/class.order.php";
include_once "classes/class.worker.php";
$method = $_SERVER['REQUEST_METHOD'];
$paths = explode("/", $_GET['_url']);
//для удаления пустого элемента в начале массива после Explode
array_shift($paths);
$resource = array_shift($paths);

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
else  {
// We only handle resources under 'orders'
    header('HTTP/1.1 404 Not Found');
}

// Workers
function handle_workers_base($method)
{
    $api = new GyroWorker();

    switch ($method) {
        case 'POST':
            //TODO: в дальнейшем тут нужны проверки на аргументы
            $first_name = $_POST["first_name"];
            $second_name = $_POST["second_name"];
            $middle_name = $_POST["middle_name"];
            return_json($api->insert($first_name, $second_name, $middle_name));
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
    $api = new GyroWorker();

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
    $api = new Order();

    switch ($method) {
        case 'POST':
            //TODO: в дальнейшем тут нужны проверки на аргументы
            $worker_id = $_POST["worker_id"];
            $total = $_POST["total"];
            $client_name = $_POST["client_name"];
            return_json($api->insert_order($worker_id, $total, $client_name));
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
    $api = new Order();

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
