package actors

import akka.actor.typed.scaladsl.Behaviors
import akka.actor.typed.{ActorRef, Behavior}
import domain._

import java.util.UUID


object SessionActor {

    sealed trait Command
    final case class AddStory(playerId: UUID, story: String, replayTo: ActorRef[Either[Validation, Session]]) extends Command
    final case class StartGame(playerId: UUID, gameId: UUID, name: String, columnCount: Int, cards: Array[String], replayTo: ActorRef[Either[Validation, Session]]) extends Command
    final case class JoinPlayer(player: Player) extends Command
    final case class LeftPlayer(player: Player) extends Command

    final case class GetSnapshot(replayTo: ActorRef[Session]) extends Command
    final case class GetGame(replayTo: ActorRef[Either[Validation, ActorRef[GameActor.Command]]]) extends Command


    def apply(session: Session): Behavior[Command] = sessionActor(session, None)


    def sessionActor(session: Session, gameActor: Option[ActorRef[GameActor.Command]]): Behavior[Command] = {
        Behaviors.receive { (context, cmd) =>
            cmd match {
                case AddStory(playerId, story, replayTo) =>
                    replayTo ! session.addStory(playerId, story)
                    sessionActor(session, gameActor)
                case StartGame(playerId, gameId, name, columnCount, cards, replayTo) =>

                    val game = Game.create(gameId, session.owner.id, name, session.players.map(_.id).toArray, columnCount, cards)
                    game match {
                        case Right(game) =>
                            val actor = context.spawn(GameActor(game), game.id.toString)
                            replayTo ! session.setGame(playerId, gameId)
                            sessionActor(session, Some(actor))
                        case Left(err) =>
                            replayTo ! Left (err)
                            Behaviors.same
                    }

                case JoinPlayer(player) =>
                    session.joinPlayer(player)
                    sessionActor(session, gameActor)

                case LeftPlayer(player) =>
                    session.leftPlayer(player)
                    sessionActor(session, gameActor)

                case GetSnapshot(replayTo) =>
                    replayTo ! session
                    Behaviors.same

                case GetGame(replayTo) =>
                    gameActor match {
                        case Some(actor) =>
                            replayTo ! Right (actor)
                            Behaviors.same
                        case None =>
                            replayTo ! Left(GameNotExists)
                            Behaviors.same
                    }
            }
        }
    }

}
