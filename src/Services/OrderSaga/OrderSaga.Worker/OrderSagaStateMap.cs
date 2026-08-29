using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OrderSaga.Worker;

public class OrderSagaStateMap : SagaClassMap<OrderSagaState>
{
    protected override void Configure(EntityTypeBuilder<OrderSagaState> entity, ModelBuilder model)
    {
        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.OrderId);
        entity.Property(x => x.Amount).HasColumnType("numeric(18,2)");
        entity.Property(x => x.Currency).HasMaxLength(8);
        entity.Property(x => x.PaymentAttempts);
        entity.Property(x => x.CreatedAt);
        entity.Property(x => x.UpdatedAt);
    }
}