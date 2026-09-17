using SSO.Application.Specifications.Base;
using System;
using System.Collections.Generic;
using System.Text;
using SSO.Domain.Entities;
using SSO.Application.Extensions;

namespace SSO.Application.Specifications.Audit
{
    public class AuditFilterSpecification : Specification<SSO.Domain.Models.Audit.Audit>
    {
        public AuditFilterSpecification(string userId, string searchString, bool searchInOldValues, bool searchInNewValues, DateTime? start = null, DateTime? end = null)
        {
            if (start == null && end == null)
            {
                Criteria = !string.IsNullOrEmpty(searchString)
                    ? (p => (p.TableName.Contains(searchString) || (searchInOldValues && p.OldValues.Contains(searchString)) || (searchInNewValues && p.NewValues.Contains(searchString))) && (string.IsNullOrEmpty(userId) || p.UserId == userId))
                    : (p => string.IsNullOrEmpty(userId) || p.UserId == userId);
            }
            else
            {
                Criteria = (p => p.DateTime >= start.Value.ToUniversalTime().Date && p.DateTime <= end.Value.ToUniversalTime().Date.AddHours(23).AddMinutes(59));
                Criteria = !string.IsNullOrEmpty(searchString)
                    ? Criteria.And(p => (p.TableName.Contains(searchString) || (searchInOldValues && p.OldValues.Contains(searchString)) || (searchInNewValues && p.NewValues.Contains(searchString))) && (string.IsNullOrEmpty(userId) || p.UserId == userId))
                    : Criteria.And(p => string.IsNullOrEmpty(userId) || p.UserId == userId);
            }
        }
    }
}
