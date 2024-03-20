using MassTransit;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using SagaStateMachine.Service.StateInstances;

namespace SagaStateMachine.Service.StateMaps
{
    // Bu clss, OrderStateInstance tipi icin veritabanina yansitilacak tablo ve kolonlarin konfigurasyonlarini yapar.
    //SagaClassMap sınıfı, MassTransit’in EF Core ile birlikte kullanılması durumunda, state instance’larının veritabanına nasıl yansıtılacağını belirlemek için kullanılır.
    public class OrderStateMap : SagaClassMap<OrderStateInstance>
    {
        protected override void Configure(EntityTypeBuilder<OrderStateInstance> entity, ModelBuilder model)
        {
            entity.Property(x => x.BuyerId)
                .IsRequired();

            entity.Property(x => x.OrderId)
                .IsRequired();

            entity.Property(x => x.TotalPrice)
                .HasDefaultValue(0);
        }
    }
}
