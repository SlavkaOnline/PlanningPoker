package application.handlers

import actors.{SessionActor, SessionStore}
import akka.actor.typed.scaladsl.AskPattern.Askable
import akka.actor.typed.{ActorRef, ActorSystem}
import akka.util.Timeout
import application.Requests
import application.Views.sessionViewMap
import application.endpoints.SessionEndpoints._
import sttp.tapir.server.akkahttp.AkkaHttpServerInterpreter

import akka.http.scaladsl.server.Directives._
import scala.concurrent.ExecutionContext.Implicits.global
import akka.actor.typed.scaladsl.AskPattern.schedulerFromActorSystem
import java.util.UUID
import scala.concurrent.duration.DurationInt
import akka.http.scaladsl.server.Route
import sttp.tapir.server.akkahttp._

class SessionHandlers(sessionsStore: ActorRef[SessionStore.Message])(implicit system: ActorSystem[_]) {

    implicit val timeout: Timeout = 5.seconds

    val createSessionLogic = createSession.andThen {
        case (player, Requests.CreateSession(name)) => {
            for {
                result <- sessionsStore.ask(SessionStore.AddSession(UUID.randomUUID(), player.id, name, _))
            } yield Right(sessionViewMap(result))
        }
    }

    val addStoryLogic = addStory.andThen {
        case (player, (sessionId, Requests.AddStory(name))) => {
            for {
                session <- sessionsStore.ask(SessionStore.GetSession(sessionId, _))
                result <- session.ask(SessionActor.AddStory(player.id, name, _))
            } yield Right(sessionViewMap(result))
        }
    }

    val routes: Route = AkkaHttpServerInterpreter().toRoute(List(createSessionLogic, addStoryLogic))

}
