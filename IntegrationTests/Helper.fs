namespace IntegrationTests

open System
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open Microsoft.Extensions.Hosting
open Newtonsoft.Json
open WebApi.Application


[<RequireQualifiedAccess>]
module Helper =

    let login (client: HttpClient) (name: string) : Async<AuthUserModel> =
        async {
            let user = AuthUserRequest()
            user.Name <- name
            let request = new HttpRequestMessage()
            request.RequestUri <- Uri($"%s{client.BaseAddress.ToString()}api/login")
            request.Method <- HttpMethod.Post
            request.Content <- new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json")

            let! response = client.SendAsync(request) |> Async.AwaitTask
            response.EnsureSuccessStatusCode() |> ignore

            let! content =
                response.Content.ReadAsStringAsync()
                |> Async.AwaitTask

            return JsonConvert.DeserializeObject<AuthUserModel>(content)
        }

    let requestPost<'TBody, 'TResult>
        (client: HttpClient)
        (body: 'TBody)
        (token: string)
        (path: string)
        : Async<'TResult> =
        async {
            let request = new HttpRequestMessage()
            request.RequestUri <- Uri($"%s{client.BaseAddress.ToString()}api/%s{path}")
            request.Method <- HttpMethod.Post
            request.Content <- new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
            request.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)

            let! response = client.SendAsync(request) |> Async.AwaitTask
            response.EnsureSuccessStatusCode() |> ignore
            let! content =
                response.Content.ReadAsStringAsync()
                |> Async.AwaitTask

            return JsonConvert.DeserializeObject<'TResult>(content)
        }

    let requestGet<'TResult> (client: HttpClient) (token: string) (path: string) : Async<'TResult> =
        async {
            let request = new HttpRequestMessage()
            request.RequestUri <- Uri($"%s{client.BaseAddress.ToString()}api/%s{path}")
            request.Method <- HttpMethod.Get
            request.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)

            let! response = client.SendAsync(request) |> Async.AwaitTask
            response.EnsureSuccessStatusCode() |> ignore

            let! content =
                response.Content.ReadAsStringAsync()
                |> Async.AwaitTask

            return JsonConvert.DeserializeObject<'TResult>(content)
        }
