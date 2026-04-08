using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartTaskManager.Domain.Entities;

namespace SmartTaskManager.Domain.Interfaces;

public interface ITaskRepository : IRepository<BaseTask>
{
    Task<IReadOnlyCollection<BaseTask>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<BaseTask>> GetByCategoryAsync(
        Guid userId,
        string categoryName,
        CancellationToken cancellationToken = default);
}
