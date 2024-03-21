using MassTransit;
using SagaStateMachine.Service.StateInstances;
using Shared.Messages;
using Shared.OrderEvents;
using Shared.PaymentEvents;
using Shared.Settings;
using Shared.StockEvents;

namespace SagaStateMachine.Service.StateMachines
{
    public class OrderStateMachine : MassTransitStateMachine<OrderStateInstance>
    {
        //Gelebilecek event'leri bu şekilde property olara tanımlılıyor ve böylece State Mahine'de temsil ediyoruz

        public Event<OrderStartedEvent> OrderStartedEvent { get; set; } 
        public Event<StockReservedEvent> StockReservedEvent { get; set; }
        public Event<StockNotReservedEvent> StockNotReservedEvent { get; set; }
        public Event<PaymentCompletedEvent> PaymentCompletedEvent { get; set; }
        public Event<PaymentFailedEvent> PaymentFailedEvent { get; set; }

        //Bir siparişe dair state machine tarafından kullanılacak durumları bu şekilde property olarak tanımlıyoruz
        public State OrderCreated { get; set; }
        public State StockReserved { get; set; }
        public State StockNotReserved { get; set; }
        public State PaymentCompleted { get; set; }
        public State PaymentFailed { get; set; }

        public OrderStateMachine()
        {
            //Burada state instance'da ki hangi property'nin sipariş sürecindeki stat'i tutacağı bildiriliyor.Yani artık tüm eventler CurrentState property'sin de tutalacaktır.
            //State Machine şimdilik en sade haliyle ouşturulmuştur.Burada dikkat edilmesi gereken öncelikli husus veritabanına durum kaydı yapan State Instance propery'lerinden hangiinin gerçek state bilgisini tuttuğu ayarıdır.Bunu yukarıdaki gibi InstanceState fonksyionu ile gerçekleştirmekteyiz.
            InstanceState(instance => instance.CurrentState);

            //Event => Gelen event'lere göre aksiyon almamızı sağlayan bir foksiyondur
            //OrderStartedEvent => Eğer gelen event OrderStartedEvent ise aşağıdaki aksiyonları gerçekleştir
            //CorrelateBy metodu ile veritabanında(database)tutulan Order State Instance'da ki OrderId'si ile gelen event'te ki(@event) OrderId'yi kıyaslıyoruzBöylece bu kıyas netciesinde eğer ilgili instance varsa gelenin yeni bir sipariş olmadığını anlıyor ve kaydetmiyoruz.
            //SelectId de eğer ilgili instance yoksa bunun yeni bir sipariş olduğunu anlıyoruz ve SelectId metodu ile yeni bir State instance üretiyoruz.Gerçi gelen event tetikleyici olduğu için yeni bir sişarişe karşılık geleceği ve yeni bir satır ekleyeceği aşikardır.
            Event(() => OrderStartedEvent,
                orderStateInstance => orderStateInstance.CorrelateBy<int>(database => database.OrderId, @event => @event.Message.OrderId)
                .SelectId(e => Guid.NewGuid()));

            //StockReservedEvent fırlatıldığında veritabanındaki hangi correlationid değerine sahip state instance'in state'ini değiştireceğini belirtiyoruz.Aynı çalışmaları diğer eventler içinde yapıyoruz
            //Dikkat ederseniz tetikleyici event dışındaki tüm event'ler taşdıkları korelasyon değeri ile eşleşen veritabanındakiState Instance satırı üzerinde işlem gerçekleştirmektedir./gerçekleştirecektir.
            //Çünkü State Instance oluşturmak sade ve sadece tetikleyici event'in sorumluluğundadır.Diğer event'ler artık bu oluşturulmuş state instance üzerinde durum bilgisinin değişmesini sağlamaktadırlar
            //Ayrıca tanımladığımız tüm event'lerin burada tanımlanmadığına dikkatinizi çekerim.Niyahetinde State Machine'e gelecek olan event'ler sadece bunlar olacaktır.Diğerleri ise State Mahine tarafından servislere publish yahut send edilecek event'lerdir.O yüzden state machinetarafından consume edilecek event2ler burada tanımlanırken, gönderilecek event'ler mantıken tanımlanmamıştır!
            Event(() => StockReservedEvent,
                orderStateInstance => orderStateInstance.CorrelateById(@event => @event.Message.CorrelationId));

            Event(() => StockNotReservedEvent,
                orderStateInstance => orderStateInstance.CorrelateById(@event => @event.Message.CorrelationId));

            Event(() => PaymentCompletedEvent,
                orderStateInstance => orderStateInstance.CorrelateById(@event => @event.Message.CorrelationId));

            Event(() => PaymentFailedEvent,
                orderStateInstance => orderStateInstance.CorrelateById(@event => @event.Message.CorrelationId));


            //Tetikleyici event geldiğinde State Machine'de ilk karşılayıcı state Initially fonksiyonu tarafından tanımlanmış olan Initial olacaktır.Initial nedir = State Machine'in başlangıç durumudur.
            Initially(When(OrderStartedEvent)
                //Burada When foknsionu ile o anki gelen eventin OrderStatedEvent olduğu kontrol ediliyor .Ve eğer event OrderStartedEvent ise Then fonksyionu içerisinde gerekli işlemler gerçekleştiriliyor.
                //Then fonksiyonunun içeriğine göz atarsanız eğer oluştrulacak olan State Instance'ın hangi property'sine tetikleyici event'e gelen hangi propert'lerin atanacağı belirtilmelidir.
                //Ayrıca Then fonksiyonunun ihtiyaç doğrultusunda ara işlem olarak ifade edilen işlem parçacıkları olabileceğini de gözlemleyebilirsiniz.
                .Then(context =>
                {
                    context.Instance.OrderId = context.Data.OrderId;
                    context.Instance.BuyerId = context.Data.BuyerId;
                    context.Instance.TotalPrice = context.Data.TotalPrice;
                    context.Instance.CreatedDate = DateTime.UtcNow;
                })
                //TransitionTo fonksiyonu ilgili siparişe ait instance'ın durumunu OrderCreated'a çekiyoruz 
                .TransitionTo(OrderCreated)
                //ve ardından Send fonksionu ile Stock.API'a OrderCreatedEvent'i göndererek haber veriyoruz.
                .Send(new Uri($"queue:{RabbitMQSettings.Stock_OrderCreatedEventQueue}"),
                context => new OrderCreatedEvent(context.Instance.CorrelationId)
                {
                    OrderItems = context.Data.OrderItems
                }));

            //Tetikleyici event'ten sonraki durumlar During fonksionu ile kontrol ederiz.O anki durum OrderCreated mi? kontrol ediyoruz.eğer 'OrderCreated' ise ve gelen event StockReservedEvent ise duruumu StockReserved'a çekiyoruz ve Stock.API'ya StockReservedEvent'i gönderiyoruz.
            During(OrderCreated,
                When(StockReservedEvent)
                .TransitionTo(StockReserved)
                .Send(new Uri($"queue:{RabbitMQSettings.Payment_StartedEventQueue}"),
                context => new PaymentStartedEvent(context.Instance.CorrelationId)
                {
                    //context.Instance ile context.Data arasındaki fark! context.ınstance veritabanındaki ilgili siparişe karşılık gelen instance satırını temsil ederken, context.Data ise o anki ilgili event'ten gelen datayı temsil eder.
                    TotalPrice = context.Instance.TotalPrice,
                    OrderItems = context.Data.OrderItems
                }),
                //Eğerki StockNotReservedEvent gelirse durumu StockNotReserved'a çekiyoruz ve bu sefer de bu siparişin başarız olduğunu haber edebilmek için Order.API'ı OrderFailedEvent ile uyarıyoruz (send)
                When(StockNotReservedEvent)
                .TransitionTo(StockNotReserved)
                .Send(new Uri($"queue:{RabbitMQSettings.Order_OrderFailedEventQueue}"),
                context => new OrderFailedEvent
                {
                    OrderId = context.Instance.OrderId,
                    Message = context.Data.Message
                }));

           
            During(StockReserved,
                When(PaymentCompletedEvent)
                .TransitionTo(PaymentCompleted)
                .Send(new Uri($"queue:{RabbitMQSettings.Order_OrderCompletedEventQueue}"),
                context => new OrderCompletedEvent
                {
                    OrderId = context.Instance.OrderId
                })
                .Finalize(),
                When(PaymentFailedEvent)
                .TransitionTo(PaymentFailed)
                .Send(new Uri($"queue:{RabbitMQSettings.Order_OrderFailedEventQueue}"),
                context => new OrderFailedEvent
                {
                    OrderId = context.Instance.OrderId,
                    Message = context.Data.Message
                })
                .Send(new Uri($"queue:{RabbitMQSettings.Stock_RollbackMessageQueue}"),
                context => new StockRollbackMessage
                {
                    OrderItems = context.Data.OrderItems
                }));

            SetCompletedWhenFinalized();
        }
    }
}
