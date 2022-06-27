// See https://aka.ms/new-console-template for more information
using Entities;
using Infrastructure;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Timers;

Semaphore sem = new Semaphore(4, 4);

try
{
    //ConsumeMessage();
    ConsumeMessageBatch();
}
catch { }

void ConsumeMessageBatch()
{
    var factory = new ConnectionFactory() { HostName = "localhost" };
    factory.AutomaticRecoveryEnabled = true;
    List<Hashes> _hashes = new List<Hashes>();
    using (IConnection connection = factory.CreateConnection())
    {
        using (IModel channel = connection.CreateModel())
        {
            channel.BasicQos(0, 100, false);
            channel.QueueDeclare(queue: "hashqueue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                
                var message = Encoding.UTF8.GetString(body);

                Hashes? _hash = JsonSerializer.Deserialize<Hashes>(body);
                if (_hash != null)
                {
                    _hashes.Add(_hash);
                    if (_hashes.Count >= 100)
                    {
                        

                        Thread mqThread = new Thread(() => AddDataIntoDatabaseBatch(_hashes));
                        mqThread.Start();
                        Thread.Sleep(3000);
                        channel.BasicAck(ea.DeliveryTag, true);
                        _hashes.Clear();

                    }
                }
            };

            channel.BasicConsume(queue: "hashqueue",
                                 autoAck: false,
                                 consumer: consumer);
            Console.ReadLine();
        }
    }
}

void AddDataIntoDatabaseBatch(List<Hashes> hashes)
{
    if (hashes.Count > 0)
    {
        sem.WaitOne();
        using (var context = new HashesContext())
        {
            context.BulkInsert(hashes);
        }
        sem.Release();
    }
}

void ConsumeMessage()
{
    var factory = new ConnectionFactory() { HostName = "localhost" };
    factory.AutomaticRecoveryEnabled = true;
    using (IConnection connection = factory.CreateConnection())
    {
        using (IModel channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: "hashqueue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                channel.BasicAck(ea.DeliveryTag, true);
                var message = Encoding.UTF8.GetString(body);

                Hashes? _hash = JsonSerializer.Deserialize<Hashes>(body);
                if (_hash != null)
                {
                    Thread mqThread = new Thread(() => AddDataIntoDatabase(_hash));
                    mqThread.Start();
                }
            };

            channel.BasicConsume(queue: "hashqueue",
                                 autoAck: false,
                                 consumer: consumer);
            Console.ReadLine();
        }
    }
}

void AddDataIntoDatabase(Hashes hash)
{
    sem.WaitOne();
    Console.WriteLine(" [x] Received {0} - Date{1}", hash.id, hash.date);
    using (var context = new HashesContext())
    {
        context.hash.Add(hash);
        context.SaveChanges();
    }
    //Thread.Sleep(3000);
    sem.Release();
}