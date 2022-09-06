namespace Api

open System
open System.Linq
open System.Linq.Expressions
open Microsoft.FSharp.Linq.RuntimeHelpers
open Microsoft.FSharp.Quotations
open System.Threading.Tasks
open Databases
open Gateway.Views

module PageReader =

    [<RequireQualifiedAccess>]
    module QueryOrder =
        let inline toLinq (expr : Expr<'a -> 'b>) =
          let linq = LeafExpressionConverter.QuotationToExpression expr
          let call = linq :?> MethodCallExpression
          let lambda = call.Arguments.[0] :?> LambdaExpression
          Expression.Lambda<Func<'a, 'b>>(lambda.Body, lambda.Parameters) 
        
        let Asc (order:Expr<'TEntity -> 'TOrderKey> when 'TOrderKey: comparison) (query: IQueryable<'TEntity>)  =
            let orderExpr = toLinq order
            query.OrderBy orderExpr
        
        let Desc (order:Expr<'TEntity -> 'TOrderKey> when 'TOrderKey: comparison) (query: IQueryable<'TEntity>) =
            let orderExpr = toLinq order
            query.OrderByDescending orderExpr

    type PageQuery<'TEntity, 'TView, 'TToken, 'TOrderKey> =
        { Query: DataBaseContext -> IQueryable<'TEntity>
          Mapper: 'TEntity -> 'TView
          ExtractToken: 'TEntity -> 'TToken
          Order: IQueryable<'TEntity> -> IOrderedQueryable<'TEntity>
        }


    type IPageReader<'TView, 'TToken> =
        abstract member GetPage: DataBaseContext -> Task<PageView<'TView, 'TToken>>

    
    let GetPageReader<'TEntity, 'TView, 'TOrderKey, 'TToken when 'TToken: comparison> (query: PageQuery<'TEntity, 'TView, 'TToken, 'TOrderKey>) (token: 'TToken option) =

        
        { new IPageReader<'TView, 'TToken> with
            member this.GetPage db =

                task {
                    try 
                        let entities =
                            query.Order(query.Query(db))
                                .Take(10)
                                .ToArray()

                        let views = entities |> Array.map query.Mapper
                        let token = entities |> Array.last |> query.ExtractToken
                        return { View = views; Token = token }
                    with
                    | :? Exception as e ->
                        return! raise(e)
                }

             }
