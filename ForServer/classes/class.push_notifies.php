<?php
/**
 * Created by PhpStorm.
 * User: SnoUweR
 * Date: 13.05.2018
 * Time: 22:40
 */

use sngrl\PhpFirebaseCloudMessaging\Client;
use sngrl\PhpFirebaseCloudMessaging\Message;
use sngrl\PhpFirebaseCloudMessaging\Recipient\Device;
use sngrl\PhpFirebaseCloudMessaging\Notification;


class PushNotify
{
    private $server_key = "AAAAA33lu2c:APA91bF1iMUGnwy9lgXtIHPAEkA3EvLhHF5nO2aXS08mUs3L9hOByWJkIkTYgP8AOf6GMi6bz_4U4UZLerIy6FvxSaPwjy9b-8_xeskfmrwIGZUfI9IP2unJrfguyZbwDDAEpSa2DZ7z";
    private $client;

    public function __construct()
    {
        $this->client = new Client();
        $this->client->setApiKey($this->server_key);
        $this->client->injectGuzzleHttpClient(new \GuzzleHttp\Client());
    }

    public function SendMessage($device_token, $title, $body)
    {
        $message = new Message();
        $message->setPriority('high');
        $message->addRecipient(new Device($device_token));
        $message
            ->setNotification(new Notification($title, $body))
            ->setData(['key' => 'value'])
        ;

        $response = $this->client->send($message);

        return $response->getStatusCode();
    }
}