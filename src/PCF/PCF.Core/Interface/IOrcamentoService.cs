﻿using PCF.Core.Entities;
using PCF.Shared.Dtos;

namespace PCF.Core.Interface
{
    public interface IOrcamentoService
    {
        Task<IEnumerable<Orcamento>> GetAllAsync();

        Task<Orcamento?> GetByIdAsync(int id);

        Task<Result> DeleteAsync(int id);

        Task<Result> UpdateAsync(Orcamento orcamento);

        Task<Result<int>> AddAsync(Orcamento orcamento);

        Task<IEnumerable<OrcamentoResponseViewModel>> GetAllWithDescriptionAsync();
    }
}
