using System.Text.Json;
using System.Security.Cryptography;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Mvc;

using Npgsql;
using NpgsqlTypes;

using snhw.Data;
using snhw.Common;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace snhw
{
    public class Actions
    {
        private string connectionString = string.Empty;
        private JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions();

        private int[] dialogStatusList = { -1, 1, 2, 3, 4, 5, 6 };

        public Actions()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            // connectionString = Environment.GetEnvironmentVariable("ASPNETCORE_CONNECTIONSTRINGS__DEFAULT");
            connectionString = configuration.GetConnectionString("db_master") ??
                throw new Exception("Не удалось получить настройку подключения к БД");
        }

        #region User section

        public async Task<IResult> UserCreate([FromBody] RegistrationData regData)
        {
            using NpgsqlConnection connection = new(connectionString);

            try
            {
                await connection.OpenAsync();
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

                    string authToken = (await OpenSession(_result, connection)).AuthToken;

                    return Results.Json(new AuthResponseData() { UserId = _result, AuthToken = authToken },
                        jsonSerializerOptions, "application/json", 200);
                }
                catch (Exception ex)
                {
                    await insertTransaction.RollbackAsync();

                    return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                        jsonSerializerOptions, "application/json", 500);
                }
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                        jsonSerializerOptions, "application/json", 500);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<IResult> UserDelete(Guid userId)
        {
            using NpgsqlConnection connection = new(connectionString);

            try
            {
                connection.Open();

                bool checkForUserExists = await CheckUserForAvailabilityAsync(userId, connection);

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
                    jsonSerializerOptions, "application/json", 500);
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
                bool checkForUserExists = await CheckUserForAvailabilityAsync(userId, connection);

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
                    jsonSerializerOptions, "application/json", 500);
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
                    jsonSerializerOptions, "application/json", 200);
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    jsonSerializerOptions, "application/json", 500);
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
                    jsonSerializerOptions, "application/json", 500);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<IResult> UserSearchAsync(UserBaseData userData)
        {
            SemaphoreSlim taskSemaphore = new SemaphoreSlim(0);
            Task<IResult> userSearcCallTask = Task.Run(async () => await UserSearch(userData, taskSemaphore));
            Queues.RequestTaskQueue.Enqueue(userSearcCallTask);

            Queues.RequestTaskQueueSemaphore.Release();

            await taskSemaphore.WaitAsync();

            return await userSearcCallTask;
        }

        public async Task<IResult> UserGetAsync(Guid userId)
        {
            SemaphoreSlim taskSemaphore = new SemaphoreSlim(0);
            Task<IResult> userSearcCallTask = Task.Run(async () => await UserGet(userId, taskSemaphore));
            Queues.RequestTaskQueue.Enqueue(userSearcCallTask);

            Queues.RequestTaskQueueSemaphore.Release();

            await taskSemaphore.WaitAsync();

            return await userSearcCallTask;
        }

        #endregion

        #region Contact section

        public async Task<IResult> ContactAddAsync(Guid userId, ContactData contactData)
        {
            using NpgsqlConnection connection = new(connectionString);
            try
            {
                await connection.OpenAsync();
                await using NpgsqlTransaction insertTransaction = await connection.BeginTransactionAsync();

                try
                {
                    string sqlText = "INSERT INTO sn_user_contacts " +
                        " (user_id, contact_user_id, created, comment) " +
                        " VALUES " +
                        " (@user_id, @contact_user_id, @created, @comment) " +
                        " RETURNING id";

                    await using var insertCommand = new NpgsqlCommand(sqlText, connection, insertTransaction)
                    {
                        Parameters =
                        {
                            new("@user_id", userId),
                            new("@contact_user_id", contactData.Id),
                            new("@created", DateTimeOffset.UtcNow),
                            new("@comment", contactData.Comment)
                        }
                    };

                    object? result = await insertCommand.ExecuteScalarAsync()
                            ?? throw new Exception("Не удалось получить идентификатор пользователя. Пусто");

                    await insertTransaction.CommitAsync();

                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    await insertTransaction.RollbackAsync();

                    return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                        jsonSerializerOptions, "application/json", 500);
                }
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    jsonSerializerOptions, "application/json", 500);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<IResult> ContactUpdateAsync(Guid userId, ContactData contactData)
        {
            using NpgsqlConnection connection = new(connectionString);

            try
            {
                await connection.OpenAsync();

                bool checkForUserExists = await CheckUserForAvailabilityAsync(userId, connection);
                if (!checkForUserExists)
                    return Results.Json("Пользователь не найден", jsonSerializerOptions, "application/json", 404);

                checkForUserExists = await CheckUserForAvailabilityAsync(contactData.Id, connection);
                if (!checkForUserExists)
                    return Results.Json("Пользователь не найден", jsonSerializerOptions, "application/json", 404);

                string comment = !string.IsNullOrEmpty(contactData.Comment)
                    ? "@comment"
                    : "comment";

                string sqlText = "UPDATE sn_user_contacts " +
                    $" SET status = @status, processed = @processed, comment = {comment} " +
                    " WHERE user_id = @user_id AND contact_user_id = @contact_user_id";

                await using var updateCommand = new NpgsqlCommand(sqlText, connection)
                {
                    Parameters =
                    {
                        new("@user_id", userId),
                        new("@contact_user_id", contactData.Id),
                        new("@status", contactData.Status),
                        new("@processed", DateTimeOffset.UtcNow),
                    }
                };

                if (!string.IsNullOrEmpty(contactData.Comment))
                {
                    updateCommand.Parameters.Add(new("@comment", contactData.Comment));
                }

                await updateCommand.ExecuteNonQueryAsync();

                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    jsonSerializerOptions, "application/json", 500);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<IResult> ContactGetAsync(Guid userId, Guid contactUserId)
        {
            SemaphoreSlim taskSemaphore = new SemaphoreSlim(0);
            Task<IResult> userSearcCallTask = Task.Run(async () => await ContactGet(userId, contactUserId, taskSemaphore));
            Queues.RequestTaskQueue.Enqueue(userSearcCallTask);

            Queues.RequestTaskQueueSemaphore.Release();

            await taskSemaphore.WaitAsync();

            return await userSearcCallTask;
        }

        public async Task<IResult> ContactDeleteAsync(Guid userId, Guid contactId)
        {
            ContactData contactData = new ContactData()
            {
                Id = contactId,
                Status = -1,
            };

            return await ContactUpdateAsync(userId, contactData);
        }

        #endregion

        #region Post section

        public async Task<IResult> PostCreateAsync(Guid userId, string text)
        {
            
            PostingPerson? postingPerson = Queues.ReadyForPostingPerson.FirstOrDefault(p => p.UserId == userId)
                ?? Queues.PostingPersonsList.FirstOrDefault(p => p.UserId == userId)
                ?? null;

            if (postingPerson == null) 
            {
                postingPerson = new PostingPerson(userId);
                Queues.ReadyForPostingPerson.Add(postingPerson);
            }

            return await postingPerson.PrepareForPublish(text, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        public async Task<IResult> PostGetAsync(Guid userId, Guid postId)
        {
            SemaphoreSlim taskSemaphore = new SemaphoreSlim(0);
            Task<IResult> postGetCallTask = Task.Run(async () => await PostGet(userId, postId, taskSemaphore));
            Queues.RequestTaskQueue.Enqueue(postGetCallTask);

            Queues.RequestTaskQueueSemaphore.Release();

            await taskSemaphore.WaitAsync();

            return await postGetCallTask;
        }

        public async Task<IResult> PostDeleteAsync(Guid userId, Guid postId)
        {
            using NpgsqlConnection connection = new(connectionString);

            try
            {
                await connection.OpenAsync();

                bool checkForUserExists = await CheckUserForAvailabilityAsync(userId, connection);
                if (!checkForUserExists)
                    return Results.Json("Пользователь не найден", new System.Text.Json.JsonSerializerOptions(), "application/json", 404);

                bool checkForPostExists = await CheckPostForAvailabilityAsync(postId, connection);
                if (!checkForPostExists)
                    return Results.Json("Публикация не найдена", new System.Text.Json.JsonSerializerOptions(), "application/json", 404);

                string sqlText = "UPDATE sn_user_posts " +
                    " SET status = @status, processed = @processed " +
                    " WHERE user_id = @user_id AND post_id = @post_id";

                await using var updateCommand = new NpgsqlCommand(sqlText, connection)
                {
                    Parameters =
                    {
                        new("@user_id", userId),
                        new("@post_id", postId),
                        new("@status", -1),
                        new("@processed", DateTimeOffset.UtcNow),
                    }
                };

                await updateCommand.ExecuteNonQueryAsync();

                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    jsonSerializerOptions, "application/json", 500);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<IResult> PostUpdateAsync(Guid userId, PostEditData editData)
        {
            using NpgsqlConnection connection = new(connectionString);

            try
            {
                await connection.OpenAsync();

                bool checkForUserExists = await CheckUserForAvailabilityAsync(userId, connection);
                if (!checkForUserExists)
                    return Results.Json("Пользователь не найден", new System.Text.Json.JsonSerializerOptions(), "application/json", 404);

                bool checkForPostExists = await CheckPostForAvailabilityAsync(editData.Id, connection);
                if (!checkForPostExists)
                    return Results.Json("Публикация не найдена", new System.Text.Json.JsonSerializerOptions(), "application/json", 404);

                string sqlText = "UPDATE sn_user_posts " +
                    " SET status = @status, processed = @processed, text = @text " +
                    " WHERE user_id = @user_id AND post_id = @post_id";

                await using var updateCommand = new NpgsqlCommand(sqlText, connection)
                {
                    Parameters =
                    {
                        new("@user_id", userId),
                        new("@post_id", editData.Id),
                        new("@processed", DateTimeOffset.UtcNow),
                    }
                };

                if (!string.IsNullOrEmpty(editData.Text))
                    updateCommand.Parameters.Add(new("@text", editData.Text));
                if (editData.Status != 0)
                    updateCommand.Parameters.Add(new("@status", editData.Status));

                await updateCommand.ExecuteNonQueryAsync();

                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    jsonSerializerOptions, "application/json", 500);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<IResult> FeedGetAsync(Guid userId, IDistributedCache cache)
        {
            string cacheName = $"feed-user-{userId}";

            using NpgsqlConnection connection = new(connectionString);

            try
            {
                await connection.OpenAsync();

                bool checkForUserExists = await CheckUserForAvailabilityAsync(userId, connection);

                if (!checkForUserExists)
                    return Results.Json("Пользователь не найден", jsonSerializerOptions, "application/json", 404);

                string? feed = await cache.GetStringAsync($"feed-user-{userId}");

                if (!string.IsNullOrEmpty(feed))
                    return Results.Json(feed, jsonSerializerOptions, "application/json", 200);
                else
                    return await SelectFeedByUserIdAsync(userId, cache, cacheName);
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    jsonSerializerOptions, "application/json", 500);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        #endregion

        #region Dialog section

        public async Task<IResult> DialogCreateAsync(DialogData dialogData)
        {
            using NpgsqlConnection connection = new(connectionString);

            try
            {
                if (await CheckUserForAvailabilityAsync(dialogData.UserId, connection))
                    throw new Exception("Пользователь не найден");

                if (await CheckUserForAvailabilityAsync(dialogData.ContactId, connection))
                    throw new Exception("Адресат не найден");

                await connection.OpenAsync();
                await using NpgsqlTransaction insertTransaction = await connection.BeginTransactionAsync();

                try
                {
                    string sqlText = "INSERT INTO sn_user_dialogs " +
                        " (user_id, contact_id, status_by_user, status_by_user_time, status_by_contact, status_by_contact_time, message_id, message_created, message_text) " +
                        " VALUES " +
                        " (@user_id, @contact_id, @status_by_user, @status_by_user_time, @status_by_contact, @status_by_contact_time, @message_id, @message_created, @message_text) " +
                        " RETURNING message_id";

                    await using var insertCommand = new NpgsqlCommand(sqlText, connection, insertTransaction)
                    {
                        Parameters =
                        {
                            new("@user_id", dialogData.UserId),
                            new("@contact_id", dialogData.ContactId),
                            new("@status_by_user", 1),
                            new("@status_by_user_time", DateTime.UtcNow),
                            new("@status_by_contact", 1),
                            new("@status_by_contact_time", DateTime.UtcNow),
                            new("@message_id", Guid.NewGuid()),
                            new("@message_created", DateTime.UtcNow),
                            new("@message_text", dialogData.MessageText),
                        }
                    };

                    object? result = await insertCommand.ExecuteScalarAsync()
                        ?? throw new Exception("Не удалось получить идентификатор сообщения. Пусто");

                    if (!Guid.TryParse(result.ToString(), out Guid _result))
                        throw new Exception($"Не удалось получить идентификатор сообщения. Значение: {result}");

                    await insertTransaction.CommitAsync();

                    string authToken = (await OpenSession(_result, connection)).AuthToken;

                    return Results.Json(new AuthResponseData() { UserId = _result, AuthToken = authToken },
                        jsonSerializerOptions, "application/json", 200);
                }
                catch (Exception ex)
                {
                    await insertTransaction.RollbackAsync();

                    return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                        jsonSerializerOptions, "application/json", 500);
                }
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                        jsonSerializerOptions, "application/json", 500);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<IResult> DialogGetAsync(Guid userId, Guid contactId)
        {
            SemaphoreSlim taskSemaphore = new SemaphoreSlim(0);
            Task<IResult> dialogGetCallTask = Task.Run(async () => await DialogGet(userId, contactId, taskSemaphore));
            Queues.RequestTaskQueue.Enqueue(dialogGetCallTask);

            Queues.RequestTaskQueueSemaphore.Release();

            await taskSemaphore.WaitAsync();

            return await dialogGetCallTask;
        }

        public async Task<IResult> DialogDeleteAsync(DialogData dialogData)
        {
            using NpgsqlConnection connection = new(connectionString);

            try
            {
                await connection.OpenAsync();

                bool checkForUserExists = await CheckUserForAvailabilityAsync(dialogData.UserId, connection);
                if (!checkForUserExists)
                    return Results.Json("Пользователь не найден", new JsonSerializerOptions(), "application/json", 404);

                string sqlText = "UPDATE sn_user_dialogs " +
                    " SET status_by_user = @status_by_user, status_by_user_time = @status_by_user_time " +
                    " WHERE user_id = @user_id AND contact_id = @contact_id";

                await using var updateCommand = new NpgsqlCommand(sqlText, connection)
                {
                    Parameters =
                    {
                        new("@user_id", dialogData.UserId),
                        new("@contact_id", dialogData.ContactId),
                        new("@status_by_user", -1),
                        new("@status_by_user_time", DateTimeOffset.UtcNow),
                    }
                };

                await updateCommand.ExecuteNonQueryAsync();

                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    jsonSerializerOptions, "application/json", 500);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<IResult> DialogUpdateAsync(DialogDataEdit editData)
        {
            using NpgsqlConnection connection = new(connectionString);

            try
            {
                await connection.OpenAsync();

                bool checkForUserExists = await CheckUserForAvailabilityAsync(editData.UserId, connection);
                if (!checkForUserExists)
                    return Results.Json("Пользователь не найден", new System.Text.Json.JsonSerializerOptions(), "application/json", 404);

                string sqlText = "UPDATE sn_user_dialogs SET id = id ";

                DialogDataGet message = new DialogDataGet();
                bool readyForEditText = false;

                if (!string.IsNullOrEmpty(editData.MessageText))
                {
                    IResult dialog = await DialogGetAsync(editData.UserId, editData.ContactId);
                    var mockHttpContext = new DefaultHttpContext
                    {
                        // RequestServices needs to be set so the IResult implementation can log.
                        RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
                        Response =
                        {
                            // The default response body is Stream.Null which throws away anything that is written to it.
                            Body = new MemoryStream(),
                        },
                    };
                    dialog.ExecuteAsync(mockHttpContext);
                    mockHttpContext.Response.Body.Position = 0;
                    var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

                    message = ((List<DialogDataGet>)JsonSerializer.Deserialize(mockHttpContext.Response.Body, typeof(List<DialogDataGet>), jsonOptions))
                            .FirstOrDefault(d => d.MessageId == editData.MessageId) ??
                            throw new Exception("Нет такого сообщения");

                    if (message.MessageText != editData.MessageText)
                    {
                        sqlText += ", message_processed=@message_processed, message_text=@message_text ";
                        readyForEditText = true;
                    }
                }

                if (dialogStatusList.Contains(editData.StatusByUser))
                {
                    sqlText += ", status_by_user=@status_by_user, status_by_user_time=@status_by_user_time";
                }

                if (dialogStatusList.Contains(editData.StatusByContact))
                {
                    sqlText += ", status_by_contact=@status_by_contact, status_by_contact_time=@status_by_contact_time";
                }

                if (editData.MessageParentId != null)
                {
                    sqlText += ", message_parent_id=@message_parent_id";
                }
                
                sqlText += " WHERE user_id = @user_id AND contact_id = @contact_id AND message_id = @message_id";

                await using var updateCommand = new NpgsqlCommand(sqlText, connection)
                {
                    Parameters =
                    {
                        new("@user_id", editData.UserId),
                        new("@contact_id", editData.ContactId),
                        new("@message_id", editData.MessageId),
                    }
                };

                if (editData.MessageParentId != null)
                    updateCommand.Parameters.Add(new("@message_parent_id", editData.MessageParentId));

                if (readyForEditText)
                {
                    updateCommand.Parameters.Add(new("@message_text", editData.MessageText));
                    updateCommand.Parameters.Add(new("@message_processed", DateTime.UtcNow ));
                }

                if (dialogStatusList.Contains(editData.StatusByUser))
                {
                    updateCommand.Parameters.Add(new("@status_by_user", editData.StatusByUser));
                    updateCommand.Parameters.Add(new("@status_by_user_time", DateTime.UtcNow));
                }

                if (dialogStatusList.Contains(editData.StatusByContact))
                {
                    updateCommand.Parameters.Add(new("@status_by_contact", editData.StatusByContact));
                    updateCommand.Parameters.Add(new("@status_by_contact_time", DateTime.UtcNow));
                }

                await updateCommand.ExecuteNonQueryAsync();

                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    jsonSerializerOptions, "application/json", 500);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }


        #endregion

        #region Private section

        private async Task<IResult> UserSearch(UserBaseData userData, SemaphoreSlim semaphoreSlim)
        {

            using NpgsqlConnection connection = new(connectionString);
            try
            {
                await connection.CloseAsync();

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
                    jsonSerializerOptions, "application/json", 200);
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    jsonSerializerOptions, "application/json", 500);
            }
            finally
            {
                await connection.CloseAsync();
                semaphoreSlim.Release();
            }
        }

        private async Task<IResult> PostGet(Guid userId, Guid postId, SemaphoreSlim semaphoreSlim)
        {
            using NpgsqlConnection connection = new(connectionString);

            try
            {
                connection.Open();

                string sqlText = "SELECT " +
                    " status, created, processed, text, status " +
                    " FROM sn_user_posts " +
                    " WHERE user_id = @user_id AND post_id = @post_id";

                await using var selectCommand = new NpgsqlCommand(sqlText, connection)
                {
                    Parameters =
                    {
                        new("@user_id", userId),
                        new("@post_id", postId),
                    }
                };

                using NpgsqlDataReader reader = await selectCommand.ExecuteReaderAsync();

                if (!reader.HasRows)
                    return Results.Json("Пользователь не найден", new System.Text.Json.JsonSerializerOptions(), "application/json", 404);

                List<PostGetData> postList = new List<PostGetData>();

                while (await reader.ReadAsync())
                {
                    var _postInfo = new PostGetData()
                    {
                        Id = postId,
                        Created = reader.GetDateTime(1).Second,                        
                        Text = reader.GetString(3),
                        Status = reader.GetInt16(4)
                    };

                    if (!(await reader.IsDBNullAsync(2)))
                        _postInfo.Processed = reader.GetDateTime(2).Second;

                    postList.Add(_postInfo);
                }

                return Results.Json(postList, new System.Text.Json.JsonSerializerOptions(), "application/json", 200);
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    jsonSerializerOptions, "application/json", 500);
            }
            finally
            {
                connection.Close();
                semaphoreSlim.Release();
            }
        }

        private async Task<IResult> UserGet(Guid userId, SemaphoreSlim semaphoreSlim)
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
                    jsonSerializerOptions, "application/json", 500);
            }
            finally
            {
                connection.Close();
                semaphoreSlim.Release();
            }
        }

        private async Task<IResult> DialogGet(Guid userId, Guid contactId, SemaphoreSlim semaphoreSlim)
        {
            using NpgsqlConnection connection = new(connectionString);

            try
            {
                connection.Open();

                string sqlText = "SELECT " +
                    " user_id, contact_id, status_by_user, status_by_user_time," +
                    " status_by_contact, status_by_contact_time, message_id," +
                    " message_parent_id, message_created, message_processed, message_text" +
                    " FROM sn_user_dialogs WHERE user_id = @user_id and contact_id = @contact_id";

                await using var selectCommand = new NpgsqlCommand(sqlText, connection)
                {
                    Parameters =
                    {
                        new("@user_id", userId),
                        new("@contact_id", contactId)
                    }
                };

                using NpgsqlDataReader reader = await selectCommand.ExecuteReaderAsync();

                if (!reader.HasRows)
                    return Results.Json("Пользователь не найден", new System.Text.Json.JsonSerializerOptions(), "application/json", 404);

                var dialogInfo = new List<DialogDataGet>();

                while (reader.Read())
                {
                    dialogInfo.Add(new DialogDataGet()
                    {
                        UserId = reader.GetGuid(0),
                        ContactId = reader.GetGuid(1),
                        StatusByUser = reader.GetInt16(2),
                        StatusByUserTime = reader.GetDateTime(3).Second,
                        StatusByContact = reader.GetInt16(4),
                        StatusByContactTime = reader.GetDateTime(5).Second,
                        MessageId = reader.GetGuid(6),
                        MessageParentId = !reader.IsDBNull(7) ? reader.GetGuid(7) : null,
                        MessageCreated = reader.GetDateTime(8).Second,
                        MessageProcessed = !reader.IsDBNull(9) ? reader.GetDateTime(9).Second : null,
                        MessageText = reader.GetString(10)
                    });
                }

                return Results.Json(dialogInfo, new System.Text.Json.JsonSerializerOptions(), "application/json", 200);
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    jsonSerializerOptions, "application/json", 500);
            }
            finally
            {
                connection.Close();
                semaphoreSlim.Release();
            }
        }

        private async Task<IResult> ContactGet(Guid userId, Guid contactUserId, SemaphoreSlim semaphoreSlim)
        {
            using NpgsqlConnection connection = new(connectionString);

            try
            {
                await connection.OpenAsync();

                string sqlText = "SELECT " +
                    " user_id, contact_user_id, status, created, processed, comment " +
                    " FROM sn_user_info WHERE user_id = @user_id AND contact_user_id = @contact_user_id";

                await using var selectCommand = new NpgsqlCommand(sqlText, connection)
                {
                    Parameters =
                    {
                        new("@user_id", userId),
                        new("@contact_user_id", contactUserId)
                    }
                };

                using NpgsqlDataReader reader = await selectCommand.ExecuteReaderAsync();

                if (!reader.HasRows)
                    return Results.Json("Пользователь не найден", new System.Text.Json.JsonSerializerOptions(), "application/json", 404);

                var userInfo = new ContactData();

                while (await reader.ReadAsync())
                {
                    userInfo.Id = reader.GetGuid(1);
                    userInfo.Status = reader.GetInt16(2);
                    userInfo.Comment = reader.GetString(5);
                }

                return Results.Json(userInfo, new System.Text.Json.JsonSerializerOptions(), "application/json", 200);
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                    jsonSerializerOptions, "application/json", 500);
            }
            finally
            {
                await connection.CloseAsync();
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

        private async Task<bool> CheckUserForAvailabilityAsync(Guid userId, NpgsqlConnection connection)
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

        private async Task<bool> CheckPostForAvailabilityAsync(Guid postId, NpgsqlConnection connection)
        {
            try
            {
                string sqlText = "SELECT 1 FROM sn_user_posts WHERE post_id = @post_id";

                await using var selectCommand = new NpgsqlCommand(sqlText, connection)
                {
                    Parameters =
                    {
                        new("@post_id", postId)
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

        private async Task<IResult> SelectFeedByUserIdAsync(Guid userId, IDistributedCache cache, string cacheName)
        {
            SemaphoreSlim taskSemaphore = new SemaphoreSlim(0);
            Task<IResult> userSearcCallTask = Task.Run(async Task<IResult>? () =>
            {
                try
                {
                    using NpgsqlConnection connection = new(connectionString);

                    try
                    {
                        await connection.OpenAsync();

                        bool checkForUserExists = await CheckUserForAvailabilityAsync(userId, connection);
                        if (!checkForUserExists)
                            return Results.Json("Пользователь не найден", new System.Text.Json.JsonSerializerOptions(), "application/json", 404);

                        string sqlText = $"select * from (SELECT user_id, post_id, created, processed, text, row_number() " +
                        $"OVER(PARTITION BY user_id ORDER BY processed DESC) rn " +
                        $"FROM sn_user_posts s " +
                        $"WHERE user_id<> @user_id AND status = 1 " +
                        $"AND user_id IN (SELECT contact_user_id FROM sn_user_contacts WHERE user_id = @user_id) ) " +
                        $"WHERE rn <= 3 ORDER BY processed DESC LIMIT 1000";

                        await using var selectCommand = new NpgsqlCommand(sqlText, connection)
                        {
                            Parameters =
                            {
                                new("@user_id", userId)
                            }
                        };

                        using NpgsqlDataReader reader = await selectCommand.ExecuteReaderAsync();

                        if (!reader.HasRows)
                            return Results.Json("[]", new JsonSerializerOptions(), "application/json", 200);

                        List<FeedPostData> feedPostData = new List<FeedPostData>();

                        while (await reader.ReadAsync())
                        {
                            var _feedPost = new FeedPostData()
                            {
                                AuthorId = reader.GetGuid(0),
                                Id = reader.GetGuid(1),
                                Created = reader.GetDateTime(2).Millisecond,
                                Text = reader.GetString(4),
                                Status = 1
                            };

                            if (!(await reader.IsDBNullAsync(3)))
                                _feedPost.Processed = reader.GetDateTime(3).Millisecond;
                            
                            feedPostData.Add(_feedPost);
                        }

                        var jsonFeed = JsonSerializer.Serialize(feedPostData);

                        cache.SetString(cacheName, jsonFeed, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2) });

                        return Results.Json(jsonFeed, jsonSerializerOptions, "application/json", 200);
                    }
                    catch (Exception ex)
                    {
                        return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                            jsonSerializerOptions, "application/json", 500);
                    }
                    finally
                    {
                        await connection.CloseAsync();
                        taskSemaphore.Release();
                    }
                }
                catch (Exception ex)
                {
                    return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                            jsonSerializerOptions, "application/json", 500);
                }
            });

            Queues.RequestTaskQueue.Enqueue(userSearcCallTask);
            Queues.RequestTaskQueueSemaphore.Release();

            await taskSemaphore.WaitAsync();

            return await userSearcCallTask;
        }

        #endregion
    }
}
