using LawFlow.Data;
using LawFlow.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LawFlow.Services
{
    public class DocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DocumentService(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<List<Document>> GetDocumentsForCaseAsync(int caseId)
        {
            return await _context.Documents
                .Include(d => d.UploadedBy)
                .Where(d => d.CaseId == caseId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
        }

        public async Task<Document?> UploadDocumentAsync(int caseId, string fileName, byte[] fileBytes, string docType, string uploadedById)
        {
            var c = await _context.Cases.FindAsync(caseId);
            if (c == null) return null;

            // Ensure the upload directory exists
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            // Generate unique file path
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(uploadsDir, uniqueFileName);

            await File.WriteAllBytesAsync(filePath, fileBytes);

            var doc = new Document
            {
                CaseId = caseId,
                FileName = fileName,
                FilePath = $"/uploads/{uniqueFileName}",
                DocumentType = docType,
                UploadedById = uploadedById,
                UploadedAt = DateTime.UtcNow,
                IsApproved = docType == "FIR" || docType == "VerdictPDF" ? true : (bool?)null // FIR and Verdict are auto-approved, Evidence requires Judge approval
            };

            _context.Documents.Add(doc);

            // Log activity
            var log = new ActivityLog
            {
                UserId = uploadedById,
                Action = "Document Uploaded",
                Details = $"Uploaded {docType} document: {fileName} for case {c.CaseNumber}",
                CreatedAt = DateTime.UtcNow
            };
            _context.ActivityLogs.Add(log);

            // Notify Judge/Lawyer if evidence
            if (docType == "Evidence" && !string.IsNullOrEmpty(c.JudgeId))
            {
                var notif = new Notification
                {
                    UserId = c.JudgeId,
                    Title = "New Evidence Submitted",
                    Message = $"New evidence '{fileName}' was submitted for case {c.CaseNumber} and requires approval.",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notif);
            }

            await _context.SaveChangesAsync();
            return doc;
        }

        public async Task<bool> ReviewEvidenceAsync(int docId, string judgeId, bool approve)
        {
            var doc = await _context.Documents.Include(d => d.Case).FirstOrDefaultAsync(d => d.Id == docId);
            if (doc == null) return false;

            doc.IsApproved = approve;

            // Log
            var log = new ActivityLog
            {
                UserId = judgeId,
                Action = approve ? "Evidence Approved" : "Evidence Rejected",
                Details = $"{(approve ? "Approved" : "Rejected")} evidence document '{doc.FileName}' for case {doc.Case?.CaseNumber}",
                CreatedAt = DateTime.UtcNow
            };
            _context.ActivityLogs.Add(log);

            // Notify submitter
            var notif = new Notification
            {
                UserId = doc.UploadedById,
                Title = approve ? "Evidence Approved" : "Evidence Rejected",
                Message = $"Your evidence '{doc.FileName}' for case {doc.Case?.CaseNumber} was {(approve ? "approved" : "rejected")} by the Judge.",
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notif);

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
