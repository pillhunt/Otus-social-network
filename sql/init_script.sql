SET search_path TO baeldung, public;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS public.sn_user_info
(
	user_id uuid NOT NULL DEFAULT gen_random_uuid(),
	user_name text COLLATE pg_catalog."default",
	user_sname text COLLATE pg_catalog."default",
	user_patronimic text COLLATE pg_catalog."default",
	user_birthday date,
	user_city text COLLATE pg_catalog."default",
	user_email text COLLATE pg_catalog."default" NOT NULL UNIQUE,
	user_login text COLLATE pg_catalog."default" NOT NULL UNIQUE,
	user_password text COLLATE pg_catalog."default" NOT NULL,
	user_status smallint NOT NULL,
    user_gender smallint,
	CONSTRAINT sn_user_info_pkey PRIMARY KEY (user_id)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.sn_user_info
OWNER to baeldung;

CREATE TABLE IF NOT EXISTS public.sn_user_sessions
(
    user_session_id uuid NOT NULL,
    user_id uuid NOT NULL,
    user_session_created timestamp without time zone NOT NULL,
    user_session_duration integer NOT NULL,
    user_auth_token text COLLATE pg_catalog."default" NOT NULL,
    user_session_status boolean NOT NULL DEFAULT true,
    CONSTRAINT sn_user_sessions_pkey PRIMARY KEY (user_session_id)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.sn_user_sessions
OWNER to baeldung;