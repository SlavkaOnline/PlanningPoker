package application.endpoints

import application.dto.Views._
import application.dto.Requests._
import domain.Player
import sttp.tapir._
import sttp.tapir.json.circe._
import sttp.tapir.generic.auto._
import io.circe.generic.auto._
import sttp.capabilities.akka.AkkaStreams
import sttp.model.StatusCode
import sttp.tapir.server.{ServerEndpoint, ServerEndpointInParts}

import java.util.UUID
import scala.concurrent.Future
import akka.stream.scaladsl.{Flow, Source}
import sttp.tapir._
import sttp.capabilities.akka.AkkaStreams
import sttp.capabilities.WebSockets
import sttp.ws.WebSocketFrame



object SessionEndpoints extends BaseEndpoints {

    def currentEndpoint = "Sessions"

    val createSession: ServerEndpointInParts[Player, CreateSession, (String, CreateSession), Error, Session, Any, Future] =
        baseEndpoint.post.in(jsonBody[CreateSession]).out(jsonBody[Session]).serverLogicPart(authorize)

    val addStory: ServerEndpointInParts[Player, (UUID, AddStory), (String, UUID, AddStory), Error, Session, Any, Future] =
        baseEndpoint.post.in(path[UUID]).in("stories").in(jsonBody[AddStory]).out(jsonBody[Session]).serverLogicPart(authorize)

    val createGame: ServerEndpointInParts[Player, (UUID, StartGame), (String, UUID, StartGame), Error, Session, Any, Future] =
        baseEndpoint.post.in(path[UUID]).in("games").in(jsonBody[StartGame]).out(jsonBody[Session]).serverLogicPart(authorize)

    val moveCard: ServerEndpointInParts[Player, (UUID, UUID, UUID, MoveCard), (String, UUID, UUID, UUID, MoveCard), Error, StatusCode, Any, Future] =
        baseEndpoint.post.in(path[UUID]).in("games").in(path[UUID]).in("card").in(path[UUID]).in(jsonBody[MoveCard]).out(statusCode).serverLogicPart(authorize)

    val nextPlayer: ServerEndpointInParts[Player, (UUID, UUID), (String, UUID, UUID), Error, Game, Any, Future] =
        baseEndpoint.post.in(path[UUID]).in("games").in(path[UUID]).out(jsonBody[Game]).serverLogicPart(authorize)

    val getSession: ServerEndpointInParts[Player, UUID, (String, UUID), Error, Session, Any, Future] =
        baseEndpoint.get.in(path[UUID]).out(jsonBody[Session]).serverLogicPart(authorize)

    val getGame: ServerEndpointInParts[Player, (UUID, UUID), (String, UUID, UUID), Error, Game, Any, Future] =
        baseEndpoint.get.in(path[UUID]).in("games").in(path[UUID]).out(jsonBody[Game]).serverLogicPart(authorize)
}
