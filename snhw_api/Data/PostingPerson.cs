using Npgsql;
using SocialnetworkHomework.Common;

namespace SocialnetworkHomework.Data
{
    public class PostingPerson
    {
        private Guid userId;
        
        private Queue<PostData> textsForPosting = new Queue<PostData>();

        private List<Task> postProcessingList = new List<Task>(3);

        public Guid UserId { get => userId; }

        public PostingPerson(Guid userId)
        {
            this.userId = userId;
            Queues.ReadyForPostingPerson.Enqueue(this);
        }
    
        public async Task<IResult> PostThisText(string text, DateTime postTime)
        {
            textsForPosting.Enqueue(new PostEditData()
            {
                Id = userId,
                Created = postTime,
                Status = 1,
                Text = text
            });

            var result = await Task<IResult>.Run(() => PostText(text, postTime));

            return result;
        }

        private async Task<IResult> PostText(string text, DateTime postTime) 
        {
            using NpgsqlConnection connection = new(connectionString);

            try
            {
                connection.Open();
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
                            new("@post_id", Guid.NewGuid()),
                            new("@created", DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                            new("@text", text),
                        }
                    };

                    object? result = await insertCommand.ExecuteScalarAsync()
                        ?? throw new Exception("Не удалось получить идентификатор поста. Пусто");

                    if (!Guid.TryParse(result.ToString(), out Guid _result))
                        throw new Exception($"Не удалось получить идентификатор поста. Значение: {result}");

                    await insertTransaction.CommitAsync();

                    string authToken = OpenSession(_result, connection).Result.AuthToken;

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
                await connection.CloseAsync();
            }
        }

        public async void ProcessPostingQueue(CancellationToken cancellationToken)
        {
            while (textsForPosting.Count > 0 || !cancellationToken.IsCancellationRequested) 
            {
                if (postProcessingList.Count <= 3)
                {
                    postProcessingList.Add(Task.Run(() => PostText()));
                }
                else
                {
                    if (postProcessingList.Count > 0)
                        Task.WaitAll(postProcessingList.ToArray(), cancellationToken);
                }
            }
        }

        private async void PostText()
        {
            var post = textsForPosting.Dequeue();

            
        }
    }
}
