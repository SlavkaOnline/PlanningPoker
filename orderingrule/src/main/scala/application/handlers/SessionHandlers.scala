package application.handlers

import actors.{SessionActor, SessionStore}
import akka.actor.typed.scaladsl.AskPattern.Askable
import akka.actor.typed.{ActorRef, ActorSystem}
import akka.util.Timeout
import application.{Requests, Views}
import application.Views.sessionViewMap
import application.endpoints.SessionEndpoints._
import sttp.tapir.server.akkahttp.AkkaHttpServerInterpreter
import akka.http.scaladsl.server.Directives._

import scala.concurrent.ExecutionContext.Implicits.global
import akka.actor.typed.scaladsl.AskPattern.schedulerFromActorSystem

import java.util.UUID
import scala.concurrent.duration.DurationInt
import akka.http.scaladsl.server.Route
import application.endpoints.SessionEndpoints
import domain.Session
import sttp.model._
import sttp.tapir.server.ServerEndpoint

import scala.concurrent.Future

class SessionHandlers(sessionsStore: ActorRef[SessionStore.Message])(implicit system: ActorSystem[_]) {

    implicit val timeout: Timeout = 5.seconds
    implicit val mapper: Session => Views.Session = sessionViewMap

    val createSessionLogic: ServerEndpoint[(String, Requests.CreateSession), Error, Views.Session, Any, Future] = createSession.andThen {
        case (player, Requests.CreateSession(name)) => {
            for {
                result <- sessionsStore.ask(SessionStore.AddSession(UUID.randomUUID(), player, name, _))
            } yield Right(sessionViewMap(result))
        }
    }

    val addStoryLogic: ServerEndpoint[(String, UUID, Requests.AddStory), Error, Views.Session, Any, Future] = addStory.andThen {
        case (player, (sessionId, Requests.AddStory(name))) => {
            for {
                session <- sessionsStore.ask(SessionStore.GetSession(sessionId, _))
                result <- session.ask(SessionActor.AddStory(player.id, name, _))
            } yield SessionEndpoints.mapping(result)
        }
    }

    val routes: Route = AkkaHttpServerInterpreter().toRoute(List(createSessionLogic, addStoryLogic))

}
