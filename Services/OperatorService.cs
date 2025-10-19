using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using takip.Data;
using takip.Models;

namespace takip.Services
{
    /// <summary>
    /// Operatör yönetim servisi - Veritabanında operatör işlemleri
    /// </summary>
    public class OperatorService
    {
        private readonly ProductionDbContext _context;

        public OperatorService(ProductionDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tüm aktif operatörleri getirir
        /// </summary>
        public async Task<List<Operator>> GetActiveOperatorsAsync()
        {
            return await _context.Operators
                .Where(o => o.IsActive)
                .OrderBy(o => o.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Tüm operatörleri getirir (aktif + pasif)
        /// </summary>
        public async Task<List<Operator>> GetAllOperatorsAsync()
        {
            return await _context.Operators
                .OrderBy(o => o.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Yeni operatör ekler
        /// </summary>
        public async Task<Operator> AddOperatorAsync(string name)
        {
            var existingOperator = await _context.Operators
                .FirstOrDefaultAsync(o => o.Name.ToLower() == name.ToLower());

            if (existingOperator != null)
            {
                if (!existingOperator.IsActive)
                {
                    existingOperator.IsActive = true;
                    existingOperator.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return existingOperator;
                }
                else
                {
                    throw new InvalidOperationException($"'{name}' isimli operatör zaten mevcut!");
                }
            }

            var newOperator = new Operator
            {
                Name = name.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Operators.Add(newOperator);
            await _context.SaveChangesAsync();
            return newOperator;
        }

        /// <summary>
        /// Operatörü pasif yapar (silmez, sadece gizler)
        /// </summary>
        public async Task<bool> DeactivateOperatorAsync(int operatorId)
        {
            var operatorToDeactivate = await _context.Operators.FindAsync(operatorId);
            if (operatorToDeactivate == null)
                return false;

            operatorToDeactivate.IsActive = false;
            operatorToDeactivate.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Operatörü aktif yapar
        /// </summary>
        public async Task<bool> ActivateOperatorAsync(int operatorId)
        {
            var operatorToActivate = await _context.Operators.FindAsync(operatorId);
            if (operatorToActivate == null)
                return false;

            operatorToActivate.IsActive = true;
            operatorToActivate.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Operatörü tamamen siler (veritabanından kaldırır)
        /// </summary>
        public async Task<bool> DeleteOperatorAsync(int operatorId)
        {
            var operatorToDelete = await _context.Operators.FindAsync(operatorId);
            if (operatorToDelete == null)
                return false;

            _context.Operators.Remove(operatorToDelete);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Operatör adını günceller
        /// </summary>
        public async Task<bool> UpdateOperatorNameAsync(int operatorId, string newName)
        {
            var operatorToUpdate = await _context.Operators.FindAsync(operatorId);
            if (operatorToUpdate == null)
                return false;

            var existingOperator = await _context.Operators
                .FirstOrDefaultAsync(o => o.Name.ToLower() == newName.ToLower() && o.Id != operatorId);

            if (existingOperator != null)
            {
                throw new InvalidOperationException($"'{newName}' isimli operatör zaten mevcut!");
            }

            operatorToUpdate.Name = newName.Trim();
            operatorToUpdate.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Operatör ID'sine göre operatör getirir
        /// </summary>
        public async Task<Operator?> GetOperatorByIdAsync(int operatorId)
        {
            return await _context.Operators.FindAsync(operatorId);
        }

        /// <summary>
        /// Operatör adına göre operatör getirir
        /// </summary>
        public async Task<Operator?> GetOperatorByNameAsync(string name)
        {
            return await _context.Operators
                .FirstOrDefaultAsync(o => o.Name.ToLower() == name.ToLower());
        }
    }
}
