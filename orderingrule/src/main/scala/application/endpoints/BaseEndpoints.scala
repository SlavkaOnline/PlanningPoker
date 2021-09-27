package application.endpoints

import domain.Player
import sttp.model.StatusCode
import sttp.tapir._

import java.util.UUID
import scala.concurrent.Future
import scala.concurrent.ExecutionContext.Implicits.global
trait BaseEndpoints {

    private val id = UUID.randomUUID();
    case class AuthToken(token: String)
    case class Error(msg: String, statusCode: StatusCode) extends Exception
    def currentEndpoint: String
    val error: EndpointOutput[Error] = stringBody.and(statusCode).mapTo[Error]
    val baseEndpoint: Endpoint[String, Error, Unit, Any] =
        endpoint.in(auth.bearer[String]())
            .in("api")
            .in("1.0")
            .in(currentEndpoint)
            .errorOut(error)



    def authorize(token: String): Future[Either[Error, Player]] = Future {
        if (token == "secret")
            Right(Player(id, "Spock", ""))
        else
            Left(Error("Not Access", StatusCode.Unauthorized)) // error code
    }
}
