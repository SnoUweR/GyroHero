
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
-- Описание для таблицы orders
--
DROP TABLE IF EXISTS orders;
CREATE TABLE orders (
  OrderID INT(11) UNSIGNED NOT NULL,
  Total DECIMAL(10, 0) DEFAULT NULL,
  BeginTime DATETIME DEFAULT NULL,
  ClientName VARCHAR(255) DEFAULT NULL,
  PRIMARY KEY (OrderID)
)
ENGINE = INNODB
CHARACTER SET utf8
COLLATE utf8_general_ci
ROW_FORMAT = DYNAMIC;

--
-- Описание для таблицы workers
--
DROP TABLE IF EXISTS workers;
CREATE TABLE workers (
  WorkerID INT(11) UNSIGNED NOT NULL AUTO_INCREMENT,
  FirstName VARCHAR(255) DEFAULT NULL,
  SecondName VARCHAR(255) DEFAULT NULL,
  MiddleName VARCHAR(255) DEFAULT NULL,
  PRIMARY KEY (WorkerID)
)
ENGINE = INNODB
AUTO_INCREMENT = 1
CHARACTER SET utf8
COLLATE utf8_general_ci
ROW_FORMAT = DYNAMIC;

-- 
-- Создание соответствующего пользователя для бд
--
CREATE USER 'gyrohero'@'%' IDENTIFIED BY 'somepassword';
GRANT ALL PRIVILEGES ON gyro_hero.* TO 'gyrohero'@'%';
