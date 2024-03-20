using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using SagaStateMachine.Service.StateMaps;

namespace SagaStateMachine.Service.StateDbContexts
{
    // Bu clss, OrderStateInstance tipi icin veritabanina yansitilacak tablo ve kolonlarin konfigurasyonlarini yapar.
    public class OrderStateDbContext(DbContextOptions options) : SagaDbContext(options)
    {
        protected override IEnumerable<ISagaClassMap> Configurations
        {
            get
            {
                // Bu metot, OrderStateMap sınıfının veritabanına yansıtılmasını sağlar.
                yield return new OrderStateMap();
            }
        }
    }
}
