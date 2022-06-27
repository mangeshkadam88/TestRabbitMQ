using Entities;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using TestRabbitMQ.Models;

namespace TestRabbitMQ.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HashesController : ControllerBase
    {
        private HashesContext _dbContext;
        public HashesController(HashesContext context)
        {
            _dbContext = context;
        }


        [HttpPost(Name = "PostHashes")]
        public IActionResult Post()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                //channel.ExchangeDeclare(exchange: "logs", type: ExchangeType.Fanout);

                channel.QueueDeclare(queue: "hashqueue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
                
                List<Hashes> _hashes = Helpers.GetRandomHashes();
                string message = "";
                byte[] body;
                foreach (Hashes item in _hashes)
                {
                    message = JsonSerializer.Serialize(item);
                    body = Encoding.UTF8.GetBytes(message);

                    channel.BasicPublish(exchange: "",
                                         routingKey: "hashqueue",
                                         basicProperties: null,
                                         body: body);
                }
                return Ok("success");
            }
        }

        [HttpGet(Name = "GetHashes")]
        public IActionResult Get()
        {
            IEnumerable<HashDataModel> _data = from d in _dbContext.hash
                                               group d by d.date into g
                                               orderby g.Key
                                               select new HashDataModel { date = g.Key.ToShortDateString(), count = g.Count() };
            return Ok(_data);
        }

        [HttpGet()]
        [Route("GetAll")]
        public IActionResult GetAll()
        {
            IEnumerable<Hashes> _data = from d in _dbContext.hash
                                        orderby d.date
                                        select d;
            return Ok(_data);
        }
    }
}
