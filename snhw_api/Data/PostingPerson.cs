using Npgsql;
using snhw_api.Common;

namespace snhw_api.Data
{
    public class PostingPerson
    {
        private IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        private Guid userId;

        private Queue<PostEditData> textsForPosting = new Queue<PostEditData>();
        private List<PostEditData> textInPosting = new List<PostEditData>();

        public int ReadyToPublish { get => textsForPosting.Count; }

        public Guid UserId { get => userId; }

        public PostingPerson(Guid userId)
        {
            this.userId = userId;
            Queues.ReadyForPostingPerson.Add(this);            
        }
    
        public async Task<IResult> PrepareForPublish(string text, long postTime)
        {
            try
            {
                textsForPosting.Enqueue(new PostEditData()
                {
                    Id = userId,
                    Created = postTime,
                    Status = 1,
                    Text = text
                });

                SemaphoreSlim taskSemaphore = new SemaphoreSlim(0);

                Task<IResult> postCreateCallTask = Task.Run(async () => await PostText(taskSemaphore));
                Queues.PostingTaskQueue.Enqueue(postCreateCallTask);
                Queues.PostingTaskQueueSemaphore.Release();
                await taskSemaphore.WaitAsync();

                return await postCreateCallTask;
            }
            catch (Exception ex) 
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                        new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }
        }

        public async Task<IResult> PostText(SemaphoreSlim taskSemaphore) 
        {
            PostEditData? postEditData = null;

            if (textsForPosting.Count > 0)
                postEditData = textsForPosting.Dequeue();
            else
            {
                Queues.ReadyForPostingPerson.Remove(this);
                return Results.Json($"Нет сообщений для публикации",
                    new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }

            while (textInPosting.Count == 3)
            {
                await Task.Delay(2000);
            }

            textInPosting.Add(postEditData);

            Guid newPostId = Guid.NewGuid();
            try
            {
                using NpgsqlConnection connection = new(configuration.GetConnectionString("db_master"));

                try
                {
                    await connection.OpenAsync();
                    await using NpgsqlTransaction insertTransaction = await connection.BeginTransactionAsync();

                    try
                    {
                        string sqlText = "INSERT INTO sn_user_posts " +
                            " (user_id, post_id, created, text) " +
                            " VALUES " +
                            " (@user_id, @post_id, @created, @text) " +
                            " RETURNING post_id";

                        await using var insertCommand = new NpgsqlCommand(sqlText, connection, insertTransaction)
                        {
                            Parameters =
                            {
                                new("@user_id", userId),
                                new("@post_id", newPostId),
                                new("@created", DateTimeOffset.FromUnixTimeSeconds(postEditData.Created)),
                                new("@text", postEditData.Text),
                            }
                        };

                        object? result = await insertCommand.ExecuteScalarAsync()
                            ?? throw new Exception("Не удалось получить идентификатор поста. Пусто");

                        if (!Guid.TryParse(result.ToString(), out Guid _result))
                            throw new Exception($"Не удалось получить идентификатор поста. Значение: {result}");

                        await insertTransaction.CommitAsync();

                        Queues.JsutPosteduserIdList.Add(userId);

                        return Results.Json(new PostData() { Id = _result },
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
                    taskSemaphore.Release();
                    textInPosting.Remove(postEditData);
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                        new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }
        }
    }
}
