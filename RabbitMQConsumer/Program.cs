// See https://aka.ms/new-console-template for more information
using Entities;
using Infrastructure;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using static RabbitMQConsumer.LimitedConcurrecnyLevelTaskScheduler;

Semaphore sem = new Semaphore(4, 4);

try
{
    //ConsumeMessage();
    //ConsumeMessageBatch();

    ParallelTasks();
}
catch { }

void ParallelTasks()
{
    LimitedConcurrencyLevelTaskScheduler lcts = new LimitedConcurrencyLevelTaskScheduler(4);
    List<Task> tasks = new List<Task>();

    // Create a TaskFactory and pass it our custom scheduler.
    TaskFactory task_factory = new TaskFactory(lcts);
    CancellationTokenSource cts = new CancellationTokenSource();

    var factory = new ConnectionFactory() { HostName = "localhost" };
    IConnection connection = factory.CreateConnection();
    factory.AutomaticRecoveryEnabled = true;
    for (int i = 0; i < 4; i++)
    {
        Task t = task_factory.StartNew(() => ConsumeMessagev2(connection, factory), cts.Token);
        tasks.Add(t);
    }
    // Wait for the tasks to complete before displaying a completion message.
    Task.WaitAll(tasks.ToArray());
    cts.Dispose();
    Console.WriteLine("\n\nSuccessful completion.");
}

void ConsumeMessagev2(IConnection connection, ConnectionFactory factory)
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
                //Thread mqThread = new Thread(() => AddDataIntoDatabase(_hash));
                //mqThread.Start();
                AddDataIntoDatabasev2(_hash);
            }
        };

        channel.BasicConsume(queue: "hashqueue",
                             autoAck: false,
                             consumer: consumer);
        Console.ReadLine();
    }
}

void AddDataIntoDatabasev2(Hashes hash)
{
    //sem.WaitOne();
    Console.WriteLine(" [x] Received {0} - Date{1} - Thread{2}", hash.id, DateTime.Now, Thread.CurrentThread.ManagedThreadId);
    using (var context = new HashesContext())
    {
        context.hash.Add(hash);
        context.SaveChanges();
    }
    //Thread.Sleep(3000);
    //sem.Release();
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