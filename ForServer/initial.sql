
-- 
-- Установка кодировки, с использованием которой клиент будет посылать запросы на сервер
--
SET NAMES 'utf8';

-- 
-- Создание самой базы данных
--
CREATE DATABASE gyro_hero
CHARACTER SET utf8
COLLATE utf8_general_ci;

-- 
-- Установка базы данных по умолчанию
--
USE gyro_hero;


--
-- Описание для таблицы workers
--
DROP TABLE IF EXISTS workers;
CREATE TABLE workers (
  WorkerID int(11) UNSIGNED NOT NULL AUTO_INCREMENT,
  Name varchar(255) NOT NULL,
  Location varchar(255) DEFAULT NULL,
  Hash varchar(255) DEFAULT NULL,
  PRIMARY KEY (WorkerID)
)
  ENGINE = INNODB
  AUTO_INCREMENT = 4
  AVG_ROW_LENGTH = 5461
  CHARACTER SET utf8
  COLLATE utf8_general_ci
  ROW_FORMAT = DYNAMIC;

--
-- Описание для таблицы orders
--
DROP TABLE IF EXISTS orders;
CREATE TABLE orders (
  OrderID INT(11) UNSIGNED NOT NULL AUTO_INCREMENT,
  Total DECIMAL(10, 0) DEFAULT NULL,
  BeginTime datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ClientName VARCHAR(255) DEFAULT NULL,
  WorkerID int(11) UNSIGNED NOT NULL,
  PRIMARY KEY (OrderID),
  CONSTRAINT FK_orders_WorkerID FOREIGN KEY (WorkerID)
  REFERENCES gyro_hero.workers (WorkerID) ON DELETE NO ACTION ON UPDATE RESTRICT
)
  ENGINE = INNODB
  CHARACTER SET utf8
  COLLATE utf8_general_ci
  ROW_FORMAT = DYNAMIC;

-- 
-- Создание соответствующего пользователя для бд
--
CREATE USER 'gyrohero'@'%' IDENTIFIED BY 'somepassword';
GRANT ALL PRIVILEGES ON gyro_hero.* TO 'gyrohero'@'%';

CREATE
  DEFINER = 'gyrohero'@'%'
TRIGGER gyro_hero.trigger_hash
BEFORE INSERT
  ON gyro_hero.workers
FOR EACH ROW
  BEGIN
    SET NEW.Hash = MD5(CONCAT(RAND(), NEW.Name));
  END