package application.routing

import actors._
import akka.actor.typed.{ActorRef, ActorSystem}
import akka.util.Timeout
import application.Requests
import application.Views._
import akka.http.scaladsl.server.Directives._
import scala.concurrent.Future
import scala.concurrent.duration.DurationInt
import sttp.tapir.{Endpoint, endpoint}
import sttp.tapir.server.akkahttp.{AkkaHttpServerInterpreter}
import sttp.tapir._
import akka.http.scaladsl.server.Route
import sttp.tapir.json.circe._
import sttp.tapir.generic.auto._
import io.circe.generic.auto._

import java.util.UUID
import scala.concurrent.ExecutionContext.Implicits.global

class SessionRoutes(sessionsStore: ActorRef[SessionStore.Message])(implicit system: ActorSystem[_]) {

    import akka.actor.typed.scaladsl.AskPattern.schedulerFromActorSystem
    import akka.actor.typed.scaladsl.AskPattern.Askable

    implicit val timeout: Timeout = 5.seconds
    val owner: UUID = UUID.randomUUID()

    val createSessionRoute: Endpoint[Requests.CreateSession, Unit, Session, Any] =
        endpoint.post.in("sessions").in(jsonBody[Requests.CreateSession]).out(jsonBody[Session])

    val addStoryRoute: Endpoint[(UUID, Requests.AddStory), Unit, Session, Any] =
        endpoint.post.in("sessions").in(sttp.tapir.path[UUID]).in("stories").in(jsonBody[Requests.AddStory]).out(jsonBody[Session])

    val createSessionHandler: Route =
        AkkaHttpServerInterpreter().toRoute(createSessionRoute){request => {
            for {
                result <- sessionsStore.ask(SessionStore.AddSession(UUID.randomUUID(), owner, request.name, _))
            } yield Right(sessionViewMap(result))
        }}

    val addStoryHandler: Route =
        AkkaHttpServerInterpreter().toRoute(addStoryRoute){request => {
            for {
                session <- sessionsStore.ask(SessionStore.GetSession(request._1, _))
                result <- session.ask(SessionActor.AddStory(owner, request._2.name, _))
            } yield Right(sessionViewMap(result))
        }}

    val routes: Route = createSessionHandler ~ addStoryHandler
}
