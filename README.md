1. Запустить консоль из папки приложения
2. Выполнить команду docker-compose build
3. Выполнить команду docker-compose up -d
4. После запуска контейнеров открыть в Docker каждый из контейнеров postgres и выполнить на вкладке Exec:
    1. psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER"
    2. \copy sn_user_info (user_questionnaire_id, user_name, user_sname, user_patronimic, user_birthday, user_city, user_email, user_login, user_password, user_status, user_gender, user_personal_interest) FROM 'people.csv' DELIMITER ',' CSV
    3. \copy sn_user_contacts (id, user_id, contact_user_id, status, created, processed, comment) FROM 'user_contacts.csv' DELIMITER ',' CSV
    4. \copy sn_user_posts (id, user_id, post_id, status, created, processed, text) FROM 'user_posts.csv' DELIMITER ',' CSV
5. После окончания установки данных открыть в Postman сохранённую коллекцию запросов в одноимённой папке.
