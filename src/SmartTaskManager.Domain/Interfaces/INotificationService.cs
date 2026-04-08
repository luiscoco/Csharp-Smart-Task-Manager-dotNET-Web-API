using SmartTaskManager.Domain.Entities;

namespace SmartTaskManager.Domain.Interfaces;

public interface INotificationService
{
    void Notify(string message);

    void NotifyTaskCompleted(BaseTask task);

    void NotifyTaskArchived(BaseTask task);
}
