-- Script de inicialização para criação unificada dos bancos de dados do microsserviço
CREATE DATABASE inventory_db;
CREATE DATABASE invoicing_db;

GRANT ALL PRIVILEGES ON DATABASE inventory_db TO postgres;
GRANT ALL PRIVILEGES ON DATABASE invoicing_db TO postgres;
