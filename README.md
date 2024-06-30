1. Запустить консоль из папки приложения
2. Выполнить команду docker-compose build
3. Выполнить команду docker-compose up -d
4. После окончания запуска контейнеров открыть в Docker каждый из контейнеров postgres и выполнить на вкладке Exec:
	4.1 psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER"
    4.2 \copy sn_user_info (user_questionnaire_id, user_name, user_sname, user_patronimic, user_birthday, user_city, user_email, user_login, user_password, user_status, user_gender, user_personal_interest) FROM 'people.csv' DELIMITER ',' CSV