package Domain

import java.util.UUID

object Position extends Enumeration {
    type Position = Value
    val Current, TurnedLeft, TurnedRight = Value
}

case class Card(id: UUID, name: String, column: Int, position: Position.Position) {

    def moveRight(): Either[Validation, Card] = {
        position match {
            case Position.TurnedRight => Left(Validation.CardCantMovedMoreOneStep)
            case Position.Current => Right(copy(position = Position.TurnedRight, column = column + 1))
            case Position.TurnedLeft => Right(copy(position = Position.Current, column = column + 1))
        }
    }

    def moveLeft(): Either[Validation, Card] = {
        position match {
            case Position.TurnedLeft => Left(Validation.CardCantMovedMoreOneStep)
            case Position.Current => Right(copy(position = Position.TurnedLeft, column = column - 1))
            case Position.TurnedRight => Right(copy(position = Position.Current, column = column - 1))
        }
    }
    def setCurrentPosition(): Card = copy(position = Position.Current)
}
