package application.endpoints

import domain.Player
import io.circe.{Decoder, HCursor}
import sttp.model.StatusCode
import sttp.tapir._

import java.util.UUID
import scala.concurrent.Future
import scala.concurrent.ExecutionContext.Implicits.global
import pdi.jwt.{JwtAlgorithm, JwtCirce}
import io.circe.parser._

trait BaseEndpoints {

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
        val key = "48427F99-9A59-4506-914A-F826526210AE"

        implicit val decode: Decoder[Player] = (c: HCursor) => for {
            id <- c.downField("nameid").as[UUID]
            name <- c.downField("given_name").as[String]
            picture <- c.downField("picture").as[String]
        } yield Player(id, name, picture)

        try {
            JwtCirce.validate(token, key, Seq(JwtAlgorithm.HS512))
            JwtCirce.decode(token, key, Seq(JwtAlgorithm.HS512))
                .map(v => parse(v.content).flatMap(_.as[Player]))
                .toEither
                .joinRight
                .left.flatMap(_ => Left(Error("Unauthorized", StatusCode.Unauthorized)))
        } catch {
            case e: Throwable => Left(Error("Unauthorized", StatusCode.Unauthorized))
        }
    }

    def mapping[In, Out](result: Either[domain.Validation, In]) (implicit mapper: In => Out ): Either[Error, Out] = {
        result.map(mapper).left.map(err => Error(err.errorMessage, StatusCode.BadRequest))
    }
}
