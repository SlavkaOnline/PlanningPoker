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
    final case class GetGame(gameId: UUID, replayTo: ActorRef[Either[Validation, ActorRef[GameActor.Command]]]) extends Command


    def apply(session: Session, games: Map[UUID, ActorRef[GameActor.Command]]): Behavior[Command] = {
        Behaviors.receive { (context, cmd) =>
            cmd match {
                case AddStory(playerId, story, replayTo) =>
                    replayTo ! session.addStory(playerId, story)
                    apply(session, games)
                case StartGame(playerId, gameId, name, columnCount, cards, replayTo) =>

                    val game = Game.create(gameId, session.owner.id, name, session.players.map(_.id).toArray, columnCount, cards)
                    game match {
                        case Right(game) =>
                            val actor = context.spawn(GameActor(game), game.name)
                            val _games = games.updated(game.id, actor)
                            replayTo ! session.setGame(playerId, gameId)
                            apply(session, _games)
                        case Left(err) =>
                            replayTo ! Left (err)
                            Behaviors.same
                    }

                case JoinPlayer(player) =>
                    session.joinPlayer(player)
                    apply(session, games)

                case LeftPlayer(player) =>
                    session.leftPlayer(player)
                    apply(session, games)

                case GetSnapshot(replayTo) =>
                    replayTo ! session
                    Behaviors.same

                case GetGame(gameId, replayTo) =>
                    games.get(gameId) match {
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
