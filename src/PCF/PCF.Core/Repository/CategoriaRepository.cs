﻿using Microsoft.EntityFrameworkCore;
using PCF.Core.Context;
using PCF.Core.Entities;
using PCF.Core.Interface;

namespace PCF.Core.Repository
{
    public class CategoriaRepository : Repository<Categoria>, ICategoriaRepository
    {
        public CategoriaRepository(PCFDBContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<Categoria>> GetAllAsync(int usuarioId)
        {
            return await _dbContext.Categorias.Where(c => c.UsuarioId == usuarioId || c.UsuarioId == null)
                                              .ToListAsync();
        }

        public async Task<Categoria?> GetByIdAsync(int id, int usuarioId)
        {
            return await _dbContext.Categorias.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
        }

        public async Task<bool> CheckIfExistsByNomeAsync(string nome, int usuarioId)
        {
            return await _dbContext.Categorias.AnyAsync(c => c.Nome == nome && c.UsuarioId == usuarioId);
        }
    }
}