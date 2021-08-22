using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using todoruben.Common.Models;
using todoruben.Common.Responses;
using todoruben.Functions.Entities;

namespace todoruben.Functions.Functions
{
    public static class TodoApi
    {
        [FunctionName(nameof(CreateTodo))]
        public static async Task<IActionResult> CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo")] HttpRequest req,
            [Table("todo", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
            ILogger log)
        {
            
            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Todo todo = JsonConvert.DeserializeObject<Todo>(requestBody);
            if (string.IsNullOrEmpty(todo?.TaskDescription))
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSucess = false,
                    Message = "The Request must  have a TaskDescription."
                });
            }

            TodoEntity todoEntity = new TodoEntity
            {
                CreatedTime = DateTime.UtcNow,
                ETag = "*",
                IsCompleted = false,
                PartitionKey = "TODO",
                RowKey = Guid.NewGuid().ToString(),
                TaskDescription = todo.TaskDescription
            };

            TableOperation addOperation = TableOperation.Insert(todoEntity);
            await todoTable.ExecuteAsync(addOperation);

            string message = "New todo stored in table";
            log.LogInformation(message);


            return new OkObjectResult(new Response
            {
                IsSucess = true,
                Message = message,
                Result = todoEntity

            });
        }

        
        
        
        [FunctionName(nameof(UpdateTodo))]
        public static async Task<IActionResult> UpdateTodo(
       [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todo/{id}")] HttpRequest req,
       [Table("todo", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
       string id,
       ILogger log)
        {
            log.LogInformation($"Update for todo; {id}, received.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Todo todo = JsonConvert.DeserializeObject<Todo>(requestBody);

            //Validate todo id
            TableOperation findOperation = TableOperation.Retrieve<TodoEntity>("TODO", id);
            TableResult finResult = await todoTable.ExecuteAsync(findOperation);
            if (finResult.Result == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSucess = false,
                    Message = "Todo not found."
                });

            }

            //Update todo
            TodoEntity todoEntity = (TodoEntity)finResult.Result;
            todoEntity.IsCompleted = todo.IsCompleted;
            if (!string.IsNullOrEmpty(todo.TaskDescription))
            {
                todoEntity.TaskDescription = todo.TaskDescription;
            }
            
            TableOperation updateOperation = TableOperation.Replace(todoEntity);
            await todoTable.ExecuteAsync(updateOperation);                    

            string message = $"Todo: {id}, update in table.";
            log.LogInformation(message);


            return new OkObjectResult(new Response
            {
                IsSucess = true,
                Message = message,
                Result = todoEntity

            });
        }
    }
}
