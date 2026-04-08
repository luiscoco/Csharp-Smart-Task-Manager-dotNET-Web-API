using SmartTaskManager.Domain.Entities;
using SmartTaskManager.Domain.Interfaces;

namespace SmartTaskManager.Infrastructure.Notifications;

public sealed class SilentNotificationService : INotificationService
{
    public void Notify(string message)
    {
    }

    public void NotifyTaskCompleted(BaseTask task)
    {
    }

    public void NotifyTaskArchived(BaseTask task)
    {
    }
}
