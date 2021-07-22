package planning.poker
package pocker

import cats.effect.{ContextShift, IO}
import io.circe.generic.auto._
import org.http4s.dsl.io._
import org.http4s.circe.CirceEntityCodec.circeEntityEncoder
import org.http4s.{EntityDecoder, Request, Uri}
import org.http4s.circe.jsonOf
import org.http4s.client.blaze.BlazeClientBuilder

import scala.concurrent.ExecutionContext.global

object Client {

    final case class User(id: String, name: String, token: String)
    final case class LoginUser(name: String)

    implicit val cs: ContextShift[IO] = IO.contextShift(global)

    def login(user: LoginUser): IO[User] = {
        implicit val decoder: EntityDecoder[IO, User] = jsonOf[IO, User]
        BlazeClientBuilder[IO](global).resource.use { client =>
            val uri = Uri.fromString("http://planningpocker.azurewebsites.net/api/login").toOption.get
            val request: Request[IO] = Request[IO](method = POST, uri = uri)
                .withEntity(user)
            client
                .expect(request)
        }
    }

}
