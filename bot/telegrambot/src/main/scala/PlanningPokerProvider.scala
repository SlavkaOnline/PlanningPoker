import cats.effect.{ContextShift, IO}
import org.http4s.client._
import org.http4s.implicits._
import io.circe._
import io.circe.generic.auto._
import org.http4s.dsl.io._
import io.circe.syntax._
import org.http4s.blaze.http.http2.PseudoHeaders.Method
import org.http4s.circe.CirceEntityCodec.circeEntityEncoder
import org.http4s.{EntityDecoder, Request, Uri}
import org.http4s.circe.jsonOf
import org.http4s.client.blaze.BlazeClientBuilder

import scala.concurrent.ExecutionContext.global
import java.util.concurrent.Future
import scala.concurrent.ExecutionContext.global

case class User(id: String, name: String, token: String)
case class LoginUser(name: String)

object PlanningPokerProvider {

  implicit val cs: ContextShift[IO] = IO.contextShift(global)


  def login(name: String) = {
    val decoder: EntityDecoder[IO, User] = jsonOf[IO, User]
    BlazeClientBuilder[IO](global).resource.use { client =>
      val uri = Uri.fromString("http://planningpocker.azurewebsites.net/api/login").toOption.get
      val request: Request[IO] = Request[IO](method = POST, uri = uri)
        .withEntity(LoginUser(name))
      client
        .fetchAs(request)(decoder)
      }
  }
}
