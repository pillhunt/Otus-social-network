using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;
using SocialnetworkHomework.Data;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace SocialnetworkHomework
{
    public class RequestActions
    {
        string connectionString = string.Empty;


        public RequestActions()
        {
            connectionString = Environment.GetEnvironmentVariable("ASPNETCORE_CONNECTIONSTRINGS__DEFAULT");
        }

        public async Task<IResult> UserCreate([FromBody] RegistrationData regData)
        {
            using NpgsqlConnection connection = new(connectionString);

            try
            {
                connection.Open();
                await using NpgsqlTransaction insertTransaction = await connection.BeginTransactionAsync();

                try
                {
                    string sqlText = "INSERT INTO sn_user_info " +
                        " (user_id, user_login, user_password, user_status, user_email) " +
                        " VALUES " +
                        " (@user_id, @user_login, @user_password, @user_status, @user_email) " +
                        " RETURNING user_id";

                    await using var insertCommand = new NpgsqlCommand(sqlText, connection, insertTransaction)
                    {
                        Parameters =
                            {
                                new("@user_id", Guid.NewGuid()),
                                new("@user_login", regData.EMail.Split('@')[0]),
                                new("@user_password", GetHash(regData.Password)),
                                new("@user_status", 1),
                                new("@user_email", regData.EMail)
                            }
                    };

                    object? result = await insertCommand.ExecuteScalarAsync()
                        ?? throw new Exception("Не удалось получить идентификатор пользователя. Пусто");

                    if (!Guid.TryParse(result.ToString(), out Guid _result))
                        throw new Exception($"Не удалось получить идентификатор пользователя. Значение: {result}");

                    await insertTransaction.CommitAsync();

                    string authToken = OpenSession(_result, connection).Result.AuthToken;

                    return Results.Json(new AuthResponseData() { UserId = _result, AuthToken = authToken },
                        new System.Text.Json.JsonSerializerOptions() { }, "application/json", 200);
                }
                catch (Exception ex)
                {
                    await insertTransaction.RollbackAsync();

                    return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                        new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
                }
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                        new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<IResult> UserGet(Guid userId, SemaphoreSlim semaphoreSlim)
        {
            using NpgsqlConnection connection = new(connectionString);

            try
            {
                connection.Open();

                string sqlText = "SELECT " +
                    " user_id, user_name, user_sname, user_patronimic, user_birthday, " +
                    " user_city, user_email, user_gender, user_login, user_password, " +
                    " user_status, user_personal_interest " +
                    " FROM sn_user_info WHERE user_id = @user_id";

                await using var selectCommand = new NpgsqlCommand(sqlText, connection)
                {
                    Parameters =
                    {
                        new("@user_id", userId)
                    }
                };

                using NpgsqlDataReader reader = await selectCommand.ExecuteReaderAsync();

                if (!reader.HasRows)
                    return Results.Json("Пользователь не найден", new System.Text.Json.JsonSerializerOptions(), "application/json", 404);

                var userInfo = new UserInfo();

                while (reader.Read())
                {
                    userInfo.Id = reader.GetGuid(0);
                    userInfo.FirstName = !reader.IsDBNull(1) ? reader.GetString(1) : string.Empty;
                    userInfo.SecondName = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty;
                    userInfo.Patronimic = !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty;
                    userInfo.Birthday = !reader.IsDBNull(4) ? reader.GetDateTime(4) : DateTime.Now;
                    userInfo.City = !reader.IsDBNull(5) ? reader.GetString(5) : string.Empty;
                    userInfo.Email = !reader.IsDBNull(6) ? reader.GetString(6) : string.Empty;
                    userInfo.Gender = !reader.IsDBNull(7) ? (Gender)reader.GetInt16(7) : 0;
                    userInfo.Status = reader.GetInt16(10);
                    userInfo.PersonalInterest = !reader.IsDBNull(11) ? reader.GetString(11) : string.Empty;
                }

                return Results.Json(userInfo, new System.Text.Json.JsonSerializerOptions(), "application/json", 200);
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }
            finally
            {
                connection.Close();
                semaphoreSlim.Release();
            }
        }


        /// <summary>
        /// Удаление пользователя
        /// </summary>
        public async Task<IResult> UserDelete(Guid userId)
        {
            using NpgsqlConnection connection = new(connectionString);

            try
            {
                connection.Open();

                bool checkForUserExists = await CheckUserForExists(userId, connection);

                if (!checkForUserExists)
                    return Results.Json("Пользователь не найден", new System.Text.Json.JsonSerializerOptions(), "application/json", 404);

                string sqlText = "DELETE FROM sn_user_info WHERE user_id = @user_id";

                await using var deleteCommand = new NpgsqlCommand(sqlText, connection)
                {
                    Parameters =
                    {
                        new("@user_id", userId)
                    }
                };

                await deleteCommand.ExecuteNonQueryAsync();

                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<IResult> UserUpdate(Guid userId, UserEditData userInfo)
        {
            using NpgsqlConnection connection = new(connectionString);

            try
            {
                connection.Open();
                bool checkForUserExists = await CheckUserForExists(userId, connection);

                if (!checkForUserExists)
                    return Results.Json("Пользователь не найден", new System.Text.Json.JsonSerializerOptions(), "application/json", 404);

                string sqlText = "UPDATE public.sn_user_info SET " +
                    " user_name=@user_name, user_sname=@user_sname, user_patronimic=@user_patronimic, " +
                    " user_birthday=@user_birthday, user_city=@user_city, user_email=@user_email, user_gender=@user_gender, " +
                    " user_personal_interest=@user_personal_interest" +
                    " WHERE user_id = @user_id";

                await using var updateCommand = new NpgsqlCommand(sqlText, connection)
                {
                    Parameters =
                    {
                        new("@user_id", userId),
                        new("@user_name", userInfo.FirstName),
                        new("@user_sname", userInfo.SecondName),
                        new("@user_patronimic", userInfo.Patronimic),
                        new("@user_birthday", userInfo.Birthday),
                        new("@user_city", userInfo.City),
                        new("@user_email", userInfo.Email),
                        new("@user_gender", (int)userInfo.Gender),
                        new("@user_personal_interest", userInfo.PersonalInterest)
                    }
                };

                await updateCommand.ExecuteNonQueryAsync();

                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}.",
                    new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<IResult> UserLogin([FromBody] AuthRequestData authData)
        {
            using NpgsqlConnection connection = new(connectionString);

            try
            {
                connection.Open();

                string userLogin = authData.Login;
                string userPassword = string.Empty;
                string userEmail = authData.EMail;

                string authSqlStringPart = string.IsNullOrEmpty(authData.EMail) ? " user_login=@user_login " : " user_email=@user_email ";

                string sqlText = $"SELECT user_id, user_password FROM sn_user_info WHERE {authSqlStringPart}";

                await using var authCommand = new NpgsqlCommand(sqlText, connection)
                {
                    Parameters =
                    {
                        new("@user_email", authData.EMail),
                        new("@user_login", authData.Login)
                    }
                };

                Guid userId = Guid.Parse("00000000-0000-0000-0000-000000000000");

                using (NpgsqlDataReader reader = await authCommand.ExecuteReaderAsync())
                {
                    string userNotFound = string.IsNullOrEmpty(authData.EMail) ? $"с логином {userLogin}" : $"с почтой {userEmail}";

                    if (!reader.HasRows)
                        return Results.Json($"Пользователь {userNotFound} не найден.", new System.Text.Json.JsonSerializerOptions(), "application/json", 404);

                    while (reader.Read())
                    {
                        userId = reader.GetGuid(0);
                        userPassword = reader.GetString(1);
                    }

                    byte[] hashBytes = Convert.FromBase64String(userPassword);

                    byte[] salt = new byte[16];
                    Array.Copy(hashBytes, 0, salt, 0, 16);

                    Rfc2898DeriveBytes convertedPassword = new Rfc2898DeriveBytes(authData.Password, salt, 100000);
                    byte[] hash = convertedPassword.GetBytes(20);

                    for (int i = 0; i < 20; i++)
                        if (hashBytes[i + 16] != hash[i])
                            throw new UnauthorizedAccessException();
                }

                AuthResponseData result = await OpenSession(userId, connection);

                return Results.Json(result,
                    new System.Text.Json.JsonSerializerOptions() { }, "application/json", 200);
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<IResult> UserLogout([FromBody] AuthResponseData authData)
        {
            using NpgsqlConnection connection = new(connectionString);

            try
            {
                connection.Open();

                string sqlText = "UPDATE public.sn_user_sessions " +
                    " SET user_session_status = false " +
                    " WHERE user_id = @user_id AND @user_auth_token=user_auth_token";

                await using var updateCommand = new NpgsqlCommand(sqlText, connection)
                {
                    Parameters =
                    {
                        new("@user_id", authData.UserId),
                        new("@user_auth_token", authData.AuthToken)
                    }
                };

                await updateCommand.ExecuteNonQueryAsync();

                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<IResult> UserSearch(UserBaseData userData, SemaphoreSlim semaphoreSlim)
        {

            using NpgsqlConnection connection = new(connectionString);
            try
            {
                connection.Open();

                string whereRule = $" WHERE 1=1 "
                    + (string.IsNullOrEmpty(userData.FirstName) ? string.Empty : " and user_name ilike @user_name")
                    + (string.IsNullOrEmpty(userData.SecondName) ? string.Empty : " and user_sname ilike @user_sname")
                    + (string.IsNullOrEmpty(userData.Patronimic) ? string.Empty : " and user_patronimic ilike @user_patronimic")
                    + (userData.Birthday == null || !DateTime.TryParse(userData.Birthday.ToString(), out DateTime userBirthDay) ? string.Empty : " and user_birthdate=@user_birthdate")
                    + (string.IsNullOrEmpty(userData.PersonalInterest) ? string.Empty : " and user_personal_interest ilike @user_personal_interest")
                    + (userData.Gender == null ? string.Empty : " and user_gender=@user_gender")
                    + (string.IsNullOrEmpty(userData.City) ? string.Empty : " and user_city ilike @user_city");

                string sqlText = $"SELECT user_questionnaire_id, user_name, user_sname, user_patronimic, user_birthday, " +
                    $" user_city, user_gender, user_personal_interest FROM sn_user_info {whereRule}";

                var searchCommand = new NpgsqlCommand(sqlText, connection);

                if (!string.IsNullOrEmpty(userData.FirstName)) searchCommand.Parameters.AddWithValue("@user_name", userData.FirstName);
                if (!string.IsNullOrEmpty(userData.SecondName)) searchCommand.Parameters.AddWithValue("@user_sname", userData.SecondName);
                if (!string.IsNullOrEmpty(userData.Patronimic)) searchCommand.Parameters.AddWithValue("@user_patronimic", userData.Patronimic);
                if (userData.Birthday != null && DateTime.TryParse(userData.Birthday.ToString(), out userBirthDay)) searchCommand.Parameters.Add(new("@user_birthdate", userData.Birthday));
                if (!string.IsNullOrEmpty(userData.PersonalInterest)) searchCommand.Parameters.AddWithValue("@user_personal_interest", userData.PersonalInterest);
                if (userData.Gender != null) searchCommand.Parameters.Add(new("@user_gender", (int)userData.Gender));
                if (!string.IsNullOrEmpty(userData.City)) searchCommand.Parameters.AddWithValue("@user_city", userData.City);

                if (!string.IsNullOrEmpty(userData.FirstName)) searchCommand.Parameters.AddWithValue("@user_name", "%" + userData.FirstName + "%");
                if (!string.IsNullOrEmpty(userData.SecondName)) searchCommand.Parameters.AddWithValue("@user_sname", "%" + userData.SecondName + "%");
                if (!string.IsNullOrEmpty(userData.Patronimic)) searchCommand.Parameters.AddWithValue("@user_patronimic", "%" + userData.Patronimic + "%");
                if (userData.Birthday != null && DateTime.TryParse(userData.Birthday.ToString(), out userBirthDay)) searchCommand.Parameters.Add(new("@user_birthdate", userData.Birthday));
                if (!string.IsNullOrEmpty(userData.PersonalInterest)) searchCommand.Parameters.AddWithValue("@user_personal_interest", "%" + userData.PersonalInterest + "%");
                if (userData.Gender != null) searchCommand.Parameters.Add(new("@user_gender", (int)userData.Gender));
                if (!string.IsNullOrEmpty(userData.City)) searchCommand.Parameters.AddWithValue("@user_city", "%" + userData.City + "%");

                List<string> user_questionnaire_id = new List<string>();

                List<UserQuestionnaire> userQuestionnaires = new List<UserQuestionnaire>();

                using (NpgsqlDataReader reader = searchCommand.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        return Results.Json($"Пользователь не найден.", new System.Text.Json.JsonSerializerOptions(), "application/json", 404);                        
                    }


                    while (reader.Read())
                    {
                        userQuestionnaires.Add(new UserQuestionnaire()
                        {
                            QuestionnaireId = reader.GetString(0),
                            FirstName = !reader.IsDBNull(1) ? reader.GetString(1) : string.Empty,
                            SecondName = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty,
                            Patronimic = !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty,
                            Birthday = !reader.IsDBNull(4) ? reader.GetDateTime(4) : DateTime.Now,
                            City = !reader.IsDBNull(5) ? reader.GetString(5) : string.Empty,
                            Gender = !reader.IsDBNull(6) ? (Gender)reader.GetInt16(6) : 0,
                            PersonalInterest = !reader.IsDBNull(7) ? reader.GetString(7) : string.Empty,
                        });
                    }
                }

                return Results.Json(userQuestionnaires.OrderBy(q => q.QuestionnaireId).ToList(),
                    new System.Text.Json.JsonSerializerOptions() { }, "application/json", 200);
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }
            finally
            {
                connection.Close();
                semaphoreSlim.Release();
            }
        }

        private async Task<AuthResponseData> OpenSession(Guid userId, NpgsqlConnection connection)
        {
            if (userId.ToString() == "00000000-0000-0000-0000-000000000000")
                throw new Exception("Не задан идентификатор пользователя.");

            try
            {
                await using NpgsqlTransaction insertTransaction = await connection.BeginTransactionAsync();

                try
                {
                    string sqlText = "INSERT INTO sn_user_sessions " +
                        " (user_session_id, user_id, user_session_created, user_session_duration, user_auth_token, user_session_status) " +
                        " VALUES " +
                        " (@user_session_id, @user_id, @user_session_created, @user_session_duration, @user_auth_token, @user_session_status) " +
                        " RETURNING user_session_id";

                    NpgsqlParameter uploadTimeParam = new NpgsqlParameter("@user_session_created", NpgsqlDbType.Timestamp);
                    uploadTimeParam.Value = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    string authToken = Guid.NewGuid().ToString();

                    await using var insertCommand = new NpgsqlCommand(sqlText, connection, insertTransaction)
                    {
                        Parameters =
                        {
                            new("@user_session_id", Guid.NewGuid()),
                            new("@user_id", userId),
                            uploadTimeParam,
                            new("@user_session_duration", 360000),
                            new("@user_auth_token", authToken),
                            new("@user_session_status", true)
                        }
                    };

                    object? execResult = await insertCommand.ExecuteScalarAsync();

                    if (!Guid.TryParse(execResult.ToString(), out Guid _result))
                        throw new Exception($"Не удалось получить идентификатор сессии. Значение: {execResult}");

                    await insertTransaction.CommitAsync();

                    var result = new AuthResponseData() { UserId = _result, AuthToken = authToken };

                    return result;
                }
                catch (Exception ex)
                {
                    await insertTransaction.RollbackAsync();

                    throw ex; ;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<bool> CheckUserForExists(Guid userId, NpgsqlConnection connection)
        {
            try
            {
                string sqlText = "SELECT 1 FROM sn_user_info WHERE user_id = @user_id";

                await using var selectCommand = new NpgsqlCommand(sqlText, connection)
                {
                    Parameters =
                    {
                        new("@user_id", userId)
                    }
                };

                using (NpgsqlDataReader reader = await selectCommand.ExecuteReaderAsync())
                {
                    if (!reader.HasRows)
                        return false;
                    else
                        return true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string GetHash(string password)
        {
            string hashString = string.Empty;
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);

            Rfc2898DeriveBytes convertedPassword = new Rfc2898DeriveBytes(password, salt, 100000);
            byte[] hash = convertedPassword.GetBytes(20);

            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            hashString = Convert.ToBase64String(hashBytes);

            return hashString;
        }

    }
}
