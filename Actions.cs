using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SocialnetworkHomework.Data;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace SocialnetworkHomework
{
    public class Actions
    {
        private NpgsqlConnection connection;

        public Actions(NpgsqlConnection conn)
        {
            this.connection = conn;
            this.connection.Open();
            
            /*call stored procedure
            await using var cmd = dataSource.CreateCommand("CALL enroll_student($1,$2,$3,$4,$5)");

            cmd.Parameters.AddWithValue(studentId);
            cmd.Parameters.AddWithValue(courseId);
            cmd.Parameters.AddWithValue(amount);
            cmd.Parameters.AddWithValue(tax);
            cmd.Parameters.AddWithValue(invoiceDate);

            await cmd.ExecuteNonQueryAsync();

            call function

            await using var cmd = dataSource.CreateCommand("SELECT fn(?,?)");
            cmd.Parameters.AddWithValue(value1);
            cmd.Parameters.AddWithValue(value2);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) 
            {
                var result = reader.GetInt32(0);
                // ...
            }
            */
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

                    string authToken = OpenSession(_result);

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

        private string OpenSession(Guid id)
        {
            try
            {
                return Guid.NewGuid().ToString();
            }
            catch (Exception ex) 
            {
                throw new Exception($"Ошибка открытия сессии", ex);
            }
        }

        public IResult UserGet(Guid a)
        {
            try
            {
                return Results.Json(new UserInfo(), new System.Text.Json.JsonSerializerOptions(), "application/json", 200);
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }
        }
        /// <summary>
        /// Удаление пользователя
        /// </summary>
        public IResult UserDelete(Guid b)
        {
            try
            {
                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }
        }

        public IResult UserUpdate(Guid c, [FromBody] UserCommonData userInfo)
        {
            try
            {
                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }
        }

        public IResult UserLogin([FromBody] AuthRequestData authData)
        {
            try
            {
                return Results.Json(new AuthResponseData(),
                    new System.Text.Json.JsonSerializerOptions() { }, "application/json", 200);
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }
        }

        public IResult UserLogout([FromBody] AuthResponseData authData)
        {
            try
            {
                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }
        }
    }
}
