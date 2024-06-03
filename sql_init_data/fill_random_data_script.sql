UPDATE
    public.sn_user_info
SET
    user_personal_interest = original_keys.user_personal_interest,
	user_questionnaire_id = 'user_' || shuffled_data.questionnaire_id
FROM
    (SELECT 
        user_id,
        floor(random()*(select count(user_personal_interest) from public.people_pt_tmp) + 1) as rn,
		floor(random()*1000000 + 1) as questionnaire_id
    FROM 
        public.sn_user_info
    ) AS shuffled_data
    JOIN 
    (SELECT 
        user_personal_interest, 
        row_number() OVER () AS rn
    FROM 
        public.people_pt_tmp
    ) AS original_keys ON original_keys.rn = shuffled_data.rn
WHERE
    public.sn_user_info.user_id = shuffled_data.user_id;