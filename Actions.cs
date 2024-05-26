using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;
using SocialnetworkHomework.Data;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;
using System.Xml;

namespace SocialnetworkHomework
{
    public class Actions
    {
        private NpgsqlConnection connection;

        public Actions(NpgsqlConnection conn)
        {
            this.connection = conn;
            this.connection.Open();
        }

        public async Task<IResult> UserCreate([FromBody] RegistrationData regData)
        {
            try
            {
                await using NpgsqlTransaction insertTransaction = await connection.BeginTransactionAsync();

                try
                {
                    string sqlText = "INSERT INTO sn_user_info " +
                        " (user_login, user_password,user_status,user_email) " +
                        " VALUES " +
                        " (@user_login,@user_password,@user_status,@user_email) " +
                        " RETURNING user_id";

                    await using var insertCommand = new NpgsqlCommand(sqlText, connection, insertTransaction)
                    {
                        Parameters =
                    {
                        new("@user_login", regData.EMail.Split('@')[0]),
                        new("@user_password", regData.Password),
                        new("@user_status", 1),
                        new("@user_email", regData.EMail)
                    }
                    };

                    object? result = await insertCommand.ExecuteScalarAsync()
                        ?? throw new Exception("Не удалось получить идентификатор пользователя. Пусто");

                    if (!Guid.TryParse(result.ToString(), out Guid _result))
                        throw new Exception($"Не удалось получить идентификатор пользователя. Значение: {result}");

                    await insertTransaction.CommitAsync();

                    string authToken = OpenSession(_result).Result.AuthToken;

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
            catch(Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                        new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }
        }
        [HttpGet("{userId:Guid}")]
        public async Task<IResult> UserGet(Guid userId)
        {
            try
            {
                string sqlText = "SELECT " +
                    " user_id, user_name, user_sname, user_patronimic, user_birthday, " +
                    " user_city, user_email, user_gender, user_login, user_password, user_status, user_personal_interest " +
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
        }

        [HttpDelete("{userId:Guid}")]
        /// <summary>
        /// Удаление пользователя
        /// </summary>
        public async Task<IResult> UserDelete(Guid userId)
        {
            try
            {
                bool checkForUserExists = await CheckUserForExists(userId);

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
        }

        public async Task<IResult> UserUpdate(Guid userId, UserEditData userInfo)
        {
            try
            {
                bool checkForUserExists = await CheckUserForExists(userId);

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
        }

        public async Task<IResult> UserLogin([FromBody] AuthRequestData authData)
        {
            try
            {
                string userLogin = authData.Login;
                string userPassword = authData.Password;
                string userEmail = authData.EMail;

                string authSqlStringPart = string.IsNullOrEmpty(authData.EMail) ? " user_login=@user_login " : " user_email=@user_email ";
                
                string sqlText = $"SELECT user_id FROM sn_user_info WHERE {authSqlStringPart} and user_password=@user_password";

                await using var authCommand = new NpgsqlCommand(sqlText, connection)
                {
                    Parameters =
                    {
                        new("@user_email", authData.EMail),
                        new("@user_login", authData.Login),
                        new("@user_password", authData.Password)
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
                    }
                }

                AuthResponseData result = await OpenSession(userId);

                return Results.Json(result,
                    new System.Text.Json.JsonSerializerOptions() { }, "application/json", 200);
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }
        }

        public async Task<IResult> UserLogout([FromBody] AuthResponseData authData)
        {
            try
            {
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
        }


        private async Task<AuthResponseData> OpenSession(Guid userId)
        {
            if (userId.ToString() == "00000000-0000-0000-0000-000000000000")
                throw new Exception("Не задан идентификатор пользователя.");

            try
            {
                await using NpgsqlTransaction insertTransaction = await connection.BeginTransactionAsync();

                try
                {
                    string sqlText = "INSERT INTO sn_user_sessions " +
                        " (user_id, user_session_created, user_session_duration, user_auth_token, user_session_status) " +
                        " VALUES " +
                        " (@user_id, @user_session_created, @user_session_duration, @user_auth_token, @user_session_status) " +
                        " RETURNING user_session_id";

                    NpgsqlParameter uploadTimeParam = new NpgsqlParameter("@user_session_created", NpgsqlDbType.Timestamp);
                    uploadTimeParam.Value = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    string authToken = Guid.NewGuid().ToString();

                    await using var insertCommand = new NpgsqlCommand(sqlText, connection, insertTransaction)
                    {                        
                        Parameters =
                        {
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

        private async Task<bool> CheckUserForExists(Guid userId)
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
    }
}
