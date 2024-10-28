1. Запустить консоль из папки приложения
2. Получателя для клиента необходимо прописать в файле docker-compose.yml. Для каждого из клиентов snhw_client* в строке command требуется указать id получателя.
	1. Для примера: если отправить сообщение через POST /v1/post  от пользователя 37d2a778-1b60-4805-8eab-45e1a203478b, то получат 0af43625-d2bf-44b1-8d0c-2dbd95ccfe52, 0ae78439-4193-4b5b-9185-745898d76c46. Если 000006e5-8aa1-4e0d-bd1f-9a8325bfb653, то получит 00002f23-cf85-40f6-b2da-5dbdfa52d7ad.
	2. По умолчанию прописаны получатели 0af43625-d2bf-44b1-8d0c-2dbd95ccfe52 и 00002f23-cf85-40f6-b2da-5dbdfa52d7ad.
	3. Получение сообщения можно увидеть во вкладке Logs контейнера snhw_client1 или snhw_client2
3. Выполнить команду docker-compose build
4. Выполнить команду docker-compose up -d
5. После запуска контейнеров открыть в Docker каждый из контейнеров postgres и выполнить на вкладке Exec:
    1. psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER"
    2. \copy sn_user_info (user_questionnaire_id, user_name, user_sname, user_patronimic, user_birthday, user_city, user_email, user_login, user_password, user_status, user_gender, user_personal_interest) FROM 'people.csv' DELIMITER ',' CSV
    3. \copy sn_user_contacts (id, user_id, contact_user_id, status, created, processed, comment) FROM 'user_contacts.csv' DELIMITER ',' CSV
    4. \copy sn_user_posts (id, user_id, post_id, status, created, processed, text) FROM 'user_posts.csv' DELIMITER ',' CSV
6. После окончания установки данных открыть в Postman сохранённую коллекцию запросов в одноимённой папке.