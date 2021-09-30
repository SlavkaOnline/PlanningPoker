package application.endpoints


import application.Requests
import application.Views._
import domain.Player
import sttp.tapir._
import sttp.tapir.json.circe._
import sttp.tapir.generic.auto._
import io.circe.generic.auto._
import sttp.tapir.server.ServerEndpointInParts

import java.util.UUID
import scala.concurrent.Future

object SessionEndpoints extends BaseEndpoints {

    def currentEndpoint = "Sessions"

    val createSession: ServerEndpointInParts[Player, Requests.CreateSession, (String, Requests.CreateSession), Error, Session, Any, Future] =
        baseEndpoint.post.in(jsonBody[Requests.CreateSession]).out(jsonBody[Session]).serverLogicPart(authorize)

    val addStory: ServerEndpointInParts[Player, (UUID, Requests.AddStory), (String, UUID, Requests.AddStory), Error, Session, Any, Future] =
        baseEndpoint.post.in(sttp.tapir.path[UUID]).in("stories").in(jsonBody[Requests.AddStory]).out(jsonBody[Session]).serverLogicPart(authorize)

    
}
