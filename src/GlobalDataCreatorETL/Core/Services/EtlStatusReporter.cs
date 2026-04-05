using GlobalDataCreatorETL.Core.Models;

namespace GlobalDataCreatorETL.Core.Services;

/// <summary>
/// Progress reporter that translates ETL steps into UI-consumable events.
/// </summary>
public sealed class EtlStatusReporter : IProgress<EtlStep>
{
    public event EventHandler<EtlStep>? StepReported;

    public void Report(EtlStep step) =>
        StepReported?.Invoke(this, step);

    public void ReportPhase(string phase, string detail, long? rowCount = null) =>
        Report(new EtlStep { Phase = phase, Detail = detail, RowCount = rowCount });

    public void ReportError(string phase, string errorMessage) =>
        Report(new EtlStep { Phase = phase, Detail = errorMessage, IsError = true, ErrorMessage = errorMessage });
}
