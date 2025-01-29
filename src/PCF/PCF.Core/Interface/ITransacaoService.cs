﻿using PCF.Core.Entities;
using PCF.Core.Enumerables;

namespace PCF.Core.Interface
{
    public interface ITransacaoService
    {
        Task<IEnumerable<Transacao>> GetAllAsync();

        Task<IEnumerable<Transacao>> GetAllByCategoriaAsync(int categoriaId);

        Task<IEnumerable<Transacao>> GetAllByPeriodoAsync(DateTime dataInicio, DateTime? dataFin);

        Task<IEnumerable<Transacao>> GetAllByTipoAsync(TipoEnum tipo);


        Task<Transacao?> GetByIdAsync(int id);

        Task<Result> DeleteAsync(int id);

        Task<Result> UpdateAsync(Transacao transacao);

        Task<Result<int>> AddAsync(Transacao transacao);
    }
}