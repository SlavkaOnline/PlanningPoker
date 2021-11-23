package application.handlers

import actors.{GameActor, SessionActor, SessionStore}
import akka.actor.typed.scaladsl.AskPattern.Askable
import akka.actor.typed.{ActorRef, ActorSystem}
import akka.util.{ByteString, Timeout}
import application.dto.Views._
import application.endpoints.SessionEndpoints._
import sttp.tapir.server.akkahttp.AkkaHttpServerInterpreter

import scala.concurrent.ExecutionContext.Implicits.global
import akka.actor.typed.scaladsl.AskPattern.schedulerFromActorSystem

import java.util.UUID
import scala.concurrent.duration.DurationInt
import akka.http.scaladsl.server.Route
import akka.stream.scaladsl.{Flow, Sink, Source}
import application.dto.{Requests, Views}
import application.endpoints.SessionEndpoints
import sttp.capabilities
import sttp.capabilities.WebSockets
import sttp.capabilities.akka.AkkaStreams
import sttp.tapir.server.ServerEndpoint
import sttp.model.StatusCode

import scala.concurrent.Future

class SessionHandlers(sessionsStore: ActorRef[SessionStore.Message])(implicit system: ActorSystem[_]) {

    implicit val timeout: Timeout = 5.seconds
    implicit val mapperSession: domain.Session => Views.Session = sessionToViewMap
    implicit val mapperGame: domain.Game => Views.Game = gameToViewMap

    val createSessionLogic: ServerEndpoint[(String, Requests.CreateSession), Error, Views.Session, Any, Future] = createSession.andThen {
        case (player, Requests.CreateSession(name)) => {
            for {
                result <- sessionsStore.ask(SessionStore.AddSession(UUID.randomUUID(), player, name, _))
            } yield Right(sessionToViewMap(result))
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

    val createGameLogic: ServerEndpoint[(String, UUID, Requests.StartGame), SessionEndpoints.Error, Views.Session, Any, Future] = createGame.andThen {
        case (player, (sessionId,Requests.StartGame(name, columnsCount, cards))) => {
            for {
                session <- sessionsStore.ask(SessionStore.GetSession(sessionId, _))
                result <- session.ask(SessionActor.StartGame(player.id, UUID.randomUUID(), name, columnsCount, cards, _))
            } yield SessionEndpoints.mapping(result)
        }
    }

    val moveCardLogic: ServerEndpoint[(String, UUID, UUID, UUID, Requests.MoveCard), SessionEndpoints.Error, StatusCode, Any, Future] = moveCard.andThen {
        case (player, (sessionId, gameId, cardId, request)) =>
            for {
                session <- sessionsStore.ask(SessionStore.GetSession(sessionId, _))
                game <- session.ask(SessionActor.GetGame(gameId, _))
                result <- game match {
                    case Right(g) => g.ask(GameActor.MoveCard(player.id, cardId, request.direction, _))
                    case Left(err) => Future.successful(Left(err))
                }
            } yield result.map(_ => StatusCode.Ok).left.map(err => Error(err.errorMessage, StatusCode.BadRequest))
    }

    val nextPlayerLogic: ServerEndpoint[(String, UUID, UUID), SessionEndpoints.Error, Game, Any, Future] = nextPlayer.andThen {
        case (player, (sessionId, gameId)) =>
            for {
                session <- sessionsStore.ask(SessionStore.GetSession(sessionId, _))
                game <- session.ask(SessionActor.GetGame(gameId, _))
                result <- game match {
                    case Right(g) => g.ask(GameActor.Next(player.id, _))
                    case Left(err) => Future.successful(Left(err))
                }
            } yield SessionEndpoints.mapping(result)
    }

    val getSessionLogic: ServerEndpoint[(String, UUID), SessionEndpoints.Error, Session, Any, Future] = getSession.andThen {
        case (_, sessionId) =>
        for {
            session <- sessionsStore.ask(SessionStore.GetSession(sessionId, _))
            result <- session.ask(SessionActor.Get)
        } yield SessionEndpoints.mapping(Right(result))
    }

    val getGameLogic: ServerEndpoint[(String, UUID, UUID), SessionEndpoints.Error, Game, Any, Future] = getGame.andThen {
        case (_, (sessionId, gameId)) =>
            for {
                session <- sessionsStore.ask(SessionStore.GetSession(sessionId, _))
                game <- session.ask(SessionActor.GetGame(gameId, _))
                result <- game match {
                    case Right(g) => g.ask(GameActor.Get).map(Right(_))
                    case Left(err) => Future.successful(Left(err))
                }
            } yield SessionEndpoints.mapping(result)
    }

    val routes: Route = AkkaHttpServerInterpreter().toRoute(List(createSessionLogic, addStoryLogic, createGameLogic, moveCardLogic, nextPlayerLogic, getSessionLogic, getGameLogic))

}
