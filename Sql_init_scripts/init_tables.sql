SET search_path TO baeldung, public;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS public.sn_user_info
(
	user_id uuid NOT NULL DEFAULT gen_random_uuid(),
	user_questionnaire_id text,
	user_name text COLLATE pg_catalog."default",
	user_sname text COLLATE pg_catalog."default",
	user_patronimic text COLLATE pg_catalog."default",
	user_birthday date,
	user_city text COLLATE pg_catalog."default",
	user_email text COLLATE pg_catalog."default" NOT NULL UNIQUE,
	user_login text COLLATE pg_catalog."default" NOT NULL UNIQUE,
	user_password text COLLATE pg_catalog."default" NOT NULL,
	user_status smallint NOT NULL 1,
    user_gender smallint,
	CONSTRAINT sn_user_info_pkey PRIMARY KEY (user_id),
	CONSTRAINT sn_user_info_unique UNIQUE (user_id, user_email, user_login)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.sn_user_info
OWNER to baeldung;

-- Index: sn_userinfo_id_fname_sname_index

-- DROP INDEX IF EXISTS public.sn_userinfo_id_fname_sname_index;

CREATE INDEX IF NOT EXISTS sn_user_info_id_fname_sname_index
    ON public.sn_user_info USING btree
    (user_id ASC NULLS LAST)
    INCLUDE(user_name, user_sname, user_city)
    WITH (deduplicate_items=True)
    TABLESPACE pg_default;


CREATE TABLE IF NOT EXISTS public.sn_user_sessions
(
    user_session_id uuid NOT NULL,
    user_id uuid NOT NULL,
    user_session_created timestamp without time zone NOT NULL,
    user_session_duration integer NOT NULL,
    user_auth_token text COLLATE pg_catalog."default" NOT NULL,
    user_session_status boolean NOT NULL DEFAULT true,
    CONSTRAINT sn_user_sessions_pkey PRIMARY KEY (user_session_id),
	CONSTRAINT sn_user_sessions_unique UNIQUE (user_session_id)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.sn_user_sessions
OWNER to baeldung;

CREATE TABLE IF NOT EXISTS public.people_pt_tmp
(
    user_personal_interest text COLLATE pg_catalog."default"
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.people_pt_tmp
    OWNER to baeldung;
