﻿using PCF.Core.Dtos;
using PCF.Core.Entities;
using PCF.Core.Extensions;
using PCF.Core.Globalization;
using PCF.Core.Interface;

namespace PCF.Core.Services
{
    public class OrcamentoService(IAppIdentityUser appIdentityUser, IOrcamentoRepository repository, ICategoriaRepository categotiaRepository, ITransacaoRepository transacaoRepository) : IOrcamentoService
    {
        private string retorno;

        public async Task<IEnumerable<Orcamento>> GetAllAsync()
        {
            return await repository.GetAllAsync(appIdentityUser.GetUserId());
        }

        public async Task<Orcamento?> GetByIdAsync(int id)
        {
            return await repository.GetByIdAsync(id, appIdentityUser.GetUserId());
        }

        public async Task<Result> DeleteAsync(int id)
        {
            var orcamento = await GetByIdAsync(id);

            if (orcamento is null)
            {
                return Result.Fail("Orçamento inexistente");
            }

            await repository.DeleteAsync(id);
            return Result.Ok();
        }

        public async Task<Result> UpdateAsync(Orcamento orcamento)
        {
            ArgumentNullException.ThrowIfNull(orcamento);

            var orcamentoExistente = await GetByIdAsync(orcamento.Id);

            if (orcamentoExistente is null)
            {
                return Result.Fail("Orçamento inexistente");
            }

            decimal orcamentoUtilizadoCategoria = 0;
            decimal orcamentoGeral = await transacaoRepository.CheckTotalBudgetCurrentMonthAsync(appIdentityUser.GetUserId(), DateTime.Now);
            decimal orcamentoUtilizadoGeral =
                await transacaoRepository.CheckAmountUsedCurrentMonthAsync(appIdentityUser.GetUserId(), DateTime.Now);

            if (orcamentoExistente.CategoriaId != null)
            {
                //Identifica o total utilizado pela categoria no mês corrente
                orcamentoUtilizadoCategoria =
                    await transacaoRepository.CheckAmountUsedByCategoriaCurrentMonthAsync(appIdentityUser.GetUserId(), DateTime.Now,
                        orcamentoExistente.CategoriaId.Value);

                if (orcamentoUtilizadoCategoria > orcamento.ValorLimite)
                {
                    retorno = $"Ajuste seu orçamento, pois o novo valor informado {FormatoMoeda.ParaReal(orcamento.ValorLimite)} é insuficiente para os gastos totais da categoria {orcamentoExistente.Categoria.Descricao}, saldo {FormatoMoeda.ParaReal(orcamentoUtilizadoCategoria)} no mês corrente.";

                }

                if (orcamento.ValorLimite > orcamentoGeral)
                {
                    retorno = $"Ajuste seu orçamento, pois o novo valor informado {FormatoMoeda.ParaReal(orcamento.ValorLimite)} da categoria {orcamentoExistente.Categoria.Descricao}, é maior que o orçamento Geral {FormatoMoeda.ParaReal(orcamentoUtilizadoCategoria)} no mês corrente.";
                }
            }
            else
            {
                if (orcamentoUtilizadoGeral > orcamento.ValorLimite)
                {
                    retorno = $"Ajuste seu orçamento, pois o novo valor informado {FormatoMoeda.ParaReal(orcamento.ValorLimite)} é insuficiente para o total comprometido de {FormatoMoeda.ParaReal(orcamentoUtilizadoGeral)} no mês corrente.";
                }
            }

            orcamentoExistente.ValorLimite = orcamento.ValorLimite;

            await repository.UpdateAsync(orcamentoExistente);

            var result = Result.Ok()
                .WithSuccess("Orçamento atualizado com sucesso!");

            if (!string.IsNullOrEmpty(retorno))
            {
                result.Reasons.Add(new Warning(retorno));
            }

            return result;
        }

        public async Task<Result<int>> AddAsync(Orcamento orcamento)
        {
            ArgumentNullException.ThrowIfNull(orcamento);

            decimal orcamentoUtilizadoCategoria = 0;
            decimal orcamentoGeral = await transacaoRepository.CheckTotalBudgetCurrentMonthAsync(appIdentityUser.GetUserId(), DateTime.Now);

            if (orcamento.CategoriaId != null)
            {
                var categoria = await categotiaRepository.GetByIdAsync((int)orcamento.CategoriaId, appIdentityUser.GetUserId());

                if (categoria == null)
                {
                    categoria = await categotiaRepository.GetGeralByIdAsync((int)orcamento.CategoriaId);
                }

                if (categoria is null)
                {
                    return Result.Fail<int>("Categoria informada é inválida.");
                }

                if (await repository.CheckIfExistsByIdAsync(orcamento.CategoriaId.Value, appIdentityUser.GetUserId()))
                {
                    return Result.Fail<int>("Já existe um orçamento para essa categoria lançado");
                }

                orcamentoUtilizadoCategoria =
                    await transacaoRepository.CheckAmountUsedByCategoriaCurrentMonthAsync(appIdentityUser.GetUserId(), DateTime.Now,
                        orcamento.CategoriaId.Value);

                if (orcamentoUtilizadoCategoria > orcamento.ValorLimite)
                {
                    retorno = $"Ajuste seu orçamento, pois o novo valor informado {FormatoMoeda.ParaReal(orcamento.ValorLimite)} é insuficiente para os gastos totais da categoria {categoria.Descricao}, saldo {FormatoMoeda.ParaReal(orcamentoUtilizadoCategoria)} no mês corrente.";
                }

                if (orcamento.ValorLimite > orcamentoGeral)
                {
                    retorno = $"Ajuste seu orçamento, pois o novo valor informado {FormatoMoeda.ParaReal(orcamento.ValorLimite)} da categoria {categoria.Descricao}, é maior que o orçamento Geral {FormatoMoeda.ParaReal(orcamentoUtilizadoCategoria)} no mês corrente.";
                }
            }
            else
            {
                if (await repository.CheckIfExistsGeralByIdAsync(appIdentityUser.GetUserId()))
                {
                    return Result.Fail<int>("Já existe um orçamento geral lançado");
                }

                //if (orcamentoGeral < orcamento.ValorLimite)
                //{
                //    return Result.Fail<int>("Valor do orçamento maior que o disponível");
                //}
            }

            orcamento.ValorLimite = orcamento.ValorLimite;
            orcamento.UsuarioId = appIdentityUser.GetUserId();
            orcamento.CategoriaId = orcamento.CategoriaId;

            await repository.CreateAsync(orcamento);
            //return Result.Ok(orcamento.Id);
            var result = Result.Ok()
                .WithSuccess("Orçamento atualizado com sucesso!");

            if (!string.IsNullOrEmpty(retorno))
            {
                result.WithReason(new Warning(retorno));
            }

            return result;
        }

        public async Task<IEnumerable<OrcamentoResponse>> GetAllWithDescriptionAsync()
        {
            return await repository.GetOrcamentoWithCategoriaAsync(appIdentityUser.GetUserId());
        }
    }
}