using Spmis.Web.Models.Portal;

namespace Spmis.Web.Services;

public interface IWhatIfService
{
    // assumed = null -> just show the form. null result = not this student's enrollment.
    Task<WhatIfViewModel?> BuildAsync(
        Guid userId, int enrollmentId, IReadOnlyDictionary<string, string?>? assumed);
}