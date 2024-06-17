-- CREATE DATABASE snhw_database;

-- CREATE ROLE root WITH LOGIN root PASSWORD 'root';
-- GRANT ALL PRIVILEGES ON DATABASE postgres TO root;
-- GRANT ALL PRIVILEGES ON DATABASE snhw_database TO root;

-- Пользователь для реплики
CREATE ROLE replicator WITH LOGIN replication PASSWORD 'pass' ;
GRANT ALL PRIVILEGES ON DATABASE snhwdb TO replicator;