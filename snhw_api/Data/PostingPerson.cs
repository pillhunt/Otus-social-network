﻿using Microsoft.Extensions.Caching.Distributed;
using Npgsql;
using SocialnetworkHomework.Common;
using static System.Net.Mime.MediaTypeNames;

namespace SocialnetworkHomework.Data
{
    public class PostingPerson
    {
        private IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        private Guid userId;

        private Queue<PostEditData> textsForPosting = new Queue<PostEditData>();

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

                return Results.Ok();
            }
            catch (Exception ex) 
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                        new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }
        }

        public async Task<IResult> PostText() 
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
                                new("@created", postEditData.Created),
                                new("@text", postEditData.Text),
                            }
                        };

                        object? result = await insertCommand.ExecuteScalarAsync()
                            ?? throw new Exception("Не удалось получить идентификатор поста. Пусто");

                        if (!Guid.TryParse(result.ToString(), out Guid _result))
                            throw new Exception($"Не удалось получить идентификатор поста. Значение: {result}");

                        await insertTransaction.CommitAsync();

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
            catch (Exception ex)
            {
                return Results.Json($"Ошибка: {ex.Message}; Внутренняя ошибка: {ex.InnerException?.Message}",
                        new System.Text.Json.JsonSerializerOptions() { }, "application/json", 500);
            }
            
        }
    }
}
